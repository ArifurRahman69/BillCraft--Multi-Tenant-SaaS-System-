using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillCraft__Multi_Tenant_SaaS_System_.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Clients");
        }
    }
}
