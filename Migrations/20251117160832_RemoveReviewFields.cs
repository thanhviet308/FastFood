using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastFoodShop.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_IsApproved_CreatedAt",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_ProductId_CreatedAt",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "reviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ProductId",
                table: "reviews",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_IsApproved_CreatedAt",
                table: "reviews",
                columns: new[] { "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_reviews_ProductId_CreatedAt",
                table: "reviews",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
