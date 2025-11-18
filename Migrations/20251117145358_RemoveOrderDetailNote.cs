using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastFoodShop.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderDetailNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "order_detail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "order_detail",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
