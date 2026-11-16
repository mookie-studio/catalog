using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsFilterable = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeDefinitions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttributeOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeOptions_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_TenantId",
                table: "AttributeDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_TenantId_NormalizedName",
                table: "AttributeDefinitions",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_AttributeDefinitionId_NormalizedValue",
                table: "AttributeOptions",
                columns: new[] { "AttributeDefinitionId", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_TenantId",
                table: "AttributeOptions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeOptions");

            migrationBuilder.DropTable(
                name: "AttributeDefinitions");
        }
    }
}
