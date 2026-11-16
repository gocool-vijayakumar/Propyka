using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Propyka.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanPrices",
                columns: table => new
                {
                    PlanName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MonthlyPriceInr = table.Column<int>(type: "integer", nullable: false),
                    YearlyPriceInr = table.Column<int>(type: "integer", nullable: false),
                    OfferLabel = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPrices", x => x.PlanName);
                });

            migrationBuilder.InsertData(
                table: "PlanPrices",
                columns: new[] { "PlanName", "MonthlyPriceInr", "OfferLabel", "UpdatedAt", "UpdatedByUserId", "YearlyPriceInr" },
                values: new object[,]
                {
                    { "Free", 0, null, new DateTimeOffset(new DateTime(2026, 11, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0 },
                    { "Pro", 49, null, new DateTimeOffset(new DateTime(2026, 11, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 499 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanPrices");
        }
    }
}
