using BillCraft.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BillCraft.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        // Dynamic Field: EF Core প্রতিবার কোয়েরি চলার সময় এই ফিল্ড থেকেই মান রিড করবে
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatic Global Query Filter for Multi-Tenancy
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
            // Automatic TenantId Assignment on Save
            foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = CurrentTenantId;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        // 💡 Expression-এ DbContext-এর CurrentTenantId Property রেফারেন্স তৈরি করা হয়েছে
        private LambdaExpression GetTenantFilterExpression(Type type)
        {
            var parameter = Expression.Parameter(type, "e");

            // CurrentTenantId প্রপার্টি রিড করার জন্য DbContext Instance binding
            var tenantProperty = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
            var currentTenantProperty = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId)
            );

            var equal = Expression.Equal(tenantProperty, currentTenantProperty);
            return Expression.Lambda(equal, parameter);
        }
    }
}