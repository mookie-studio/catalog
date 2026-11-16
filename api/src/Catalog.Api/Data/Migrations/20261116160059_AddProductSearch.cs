using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Products",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('spanish'::regconfig, coalesce(\"Name\", '')), 'A') || setweight(to_tsvector('spanish'::regconfig, coalesce(\"Description\", '')), 'B')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchVector",
                table: "Products",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SearchVector",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Products");
        }
    }
}
