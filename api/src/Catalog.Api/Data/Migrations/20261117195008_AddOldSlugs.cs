using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOldSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OldProductSlugs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldProductSlugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OldProductSlugs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OldTenantSlugs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldTenantSlugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OldTenantSlugs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OldProductSlugs_ProductId",
                table: "OldProductSlugs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OldProductSlugs_TenantId",
                table: "OldProductSlugs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OldProductSlugs_TenantId_Slug",
                table: "OldProductSlugs",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldTenantSlugs_Slug",
                table: "OldTenantSlugs",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldTenantSlugs_TenantId",
                table: "OldTenantSlugs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OldProductSlugs");

            migrationBuilder.DropTable(
                name: "OldTenantSlugs");
        }
    }
}
