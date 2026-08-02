using BillCraft.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BillCraft.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        // Dynamic Property: EF Core ইন্টারনালি ফিল্টার রিড করার জন্য এটি ব্যবহার করবে
        public string CurrentTenantId => _tenantProvider.GetTenantId();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<TenantSetting> TenantSettings { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. One-to-One Relationship: Tenant <-> TenantSetting
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.TenantSetting)
                .WithOne(ts => ts.Tenant)
                .HasForeignKey<TenantSetting>(ts => ts.TenantId);

            // 2. One-to-Many Relationship: Tenant <-> User
            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.Users)
                .WithOne(u => u.Tenant)
                .HasForeignKey(u => u.TenantId);

            // 3. Foreign Key Mapping: Tenant <-> SubscriptionPlan
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.SubscriptionPlan)
                .WithMany(p => p.Tenants)
                .HasForeignKey(t => t.PlanId);

            // 4. Automatic Global Query Filter for Multi-Tenancy
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(GetTenantFilterExpression(entityType.ClrType));
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentTenant = CurrentTenantId;

            // Automatic TenantId Assignment on Save
            foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
            {
                if (entry.State == EntityState.Added)
                {
                    // রেজিস্ট্রেশনের সময় যদি Controller থেকে ইতোমধ্যে TenantId বসানো হয়ে থাকে, 
                    // তবে সেটি ওভাররাইট করবে না। শুধুমাত্র খালি থাকলে CurrentTenantId বসাবে।
                    if (string.IsNullOrEmpty(entry.Entity.TenantId) && !string.IsNullOrEmpty(currentTenant))
                    {
                        entry.Entity.TenantId = currentTenant;
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        // Dynamic MemberAccess Expression (মেমোরি ক্যাশিং প্রবলেম ফিক্স করা হয়েছে)
        private LambdaExpression GetTenantFilterExpression(Type type)
        {
            var parameter = Expression.Parameter(type, "e");
            var tenantProperty = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));

            var dbContextProperty = Expression.Property(
                Expression.Convert(Expression.Constant(this), typeof(ApplicationDbContext)),
                nameof(CurrentTenantId)
            );

            var equal = Expression.Equal(tenantProperty, dbContextProperty);
            return Expression.Lambda(equal, parameter);
        }
    }
}