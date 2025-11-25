using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastFoodShop.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "orders");
        }
    }
}
