using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventario_QR.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionsDependencesAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "product",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "dependence_id",
                table: "persons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "position_id",
                table: "persons",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(type: "text", nullable: false),
                    abbreviation = table.Column<string>(type: "text", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dependences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dependence = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dependences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    position = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_category_id",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_persons_dependence_id",
                table: "persons",
                column: "dependence_id");

            migrationBuilder.CreateIndex(
                name: "IX_persons_position_id",
                table: "persons",
                column: "position_id");

            migrationBuilder.AddForeignKey(
                name: "FK_persons_dependences_dependence_id",
                table: "persons",
                column: "dependence_id",
                principalTable: "dependences",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_persons_positions_position_id",
                table: "persons",
                column: "position_id",
                principalTable: "positions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_categories_category_id",
                table: "product",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_persons_dependences_dependence_id",
                table: "persons");

            migrationBuilder.DropForeignKey(
                name: "FK_persons_positions_position_id",
                table: "persons");

            migrationBuilder.DropForeignKey(
                name: "FK_product_categories_category_id",
                table: "product");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "dependences");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropIndex(
                name: "IX_product_category_id",
                table: "product");

            migrationBuilder.DropIndex(
                name: "IX_persons_dependence_id",
                table: "persons");

            migrationBuilder.DropIndex(
                name: "IX_persons_position_id",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "product");

            migrationBuilder.DropColumn(
                name: "dependence_id",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "position_id",
                table: "persons");
        }
    }
}
