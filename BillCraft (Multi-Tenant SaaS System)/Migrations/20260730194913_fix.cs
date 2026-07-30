using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillCraft__Multi_Tenant_SaaS_System_.Migrations
{
    /// <inheritdoc />
    public partial class fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanPlanId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_SubscriptionPlanPlanId",
                table: "Tenants");

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "PlanId",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanPlanId",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PlanId",
                table: "Tenants",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_SubscriptionPlans_PlanId",
                table: "Tenants",
                column: "PlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "TenantId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_SubscriptionPlans_PlanId",
                table: "Tenants");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PlanId",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlanPlanId",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "PlanId", "CreatedAt", "Description", "DurationInDays", "IsActive", "MaxInvoicesPerMonth", "MaxUsersAllowed", "Name", "Price", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, new DateTime(2026, 7, 30, 19, 26, 29, 835, DateTimeKind.Utc).AddTicks(4377), null, 30, true, 100, 5, "Free Trial", 0m, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SubscriptionPlanPlanId",
                table: "Tenants",
                column: "SubscriptionPlanPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanPlanId",
                table: "Tenants",
                column: "SubscriptionPlanPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
