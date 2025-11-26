using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastFoodShop.Migrations
{
    /// <inheritdoc />
    public partial class databases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "product_variants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "product_variants");
        }
    }
}
