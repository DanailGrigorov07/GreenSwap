using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandGoods.Data.Migrations
{
    [Migration("20260127120000_AddSiteAdvertisements")]
    public class AddSiteAdvertisements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteAdvertisements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SlotKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TargetUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAdvertisements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteAdvertisements_SlotActive",
                table: "SiteAdvertisements",
                columns: new[] { "SlotKey", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SiteAdvertisements");
        }
    }
}
