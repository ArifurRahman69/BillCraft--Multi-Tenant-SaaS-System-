using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BillCraft__Multi_Tenant_SaaS_System_.Migrations
{
    /// <inheritdoc />
    public partial class SeedSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "PlanId", "Description", "DurationInDays", "IsActive", "MaxClients", "MaxInvoicesPerMonth", "MaxProducts", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "নতুন ইউজারদের জন্য স্টার্টার প্যাকেজ", 30, true, 5, 10, 5, "Free Trial", 0.00m },
                    { 2, "ছোট ও মাঝারি ব্যবসার জন্য উপযুক্ত", 30, true, 50, 100, 50, "Standard", 999.00m },
                    { 3, "লার্জ স্কেল ব্যবসার জন্য আনলিমিটেড অ্যাক্সেস", 30, true, -1, -1, -1, "Pro Unlimited", 2499.00m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 3);
        }
    }
}
