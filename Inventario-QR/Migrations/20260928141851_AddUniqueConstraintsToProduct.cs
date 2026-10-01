using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventario_QR.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_product_code",
                table: "product",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_qr",
                table: "product",
                column: "qr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_code",
                table: "product");

            migrationBuilder.DropIndex(
                name: "IX_product_qr",
                table: "product");
        }
    }
}
