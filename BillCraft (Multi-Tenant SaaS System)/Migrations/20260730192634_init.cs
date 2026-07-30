using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillCraft__Multi_Tenant_SaaS_System_.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "PlanId", "CreatedAt", "Description", "DurationInDays", "IsActive", "MaxInvoicesPerMonth", "MaxUsersAllowed", "Name", "Price", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, new DateTime(2026, 7, 30, 19, 26, 29, 835, DateTimeKind.Utc).AddTicks(4377), null, 30, true, 100, 5, "Free Trial", 0m, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "PlanId",
                keyValue: 1);
        }
    }
}
