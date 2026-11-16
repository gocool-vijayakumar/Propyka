using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propyka.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeocodeCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeocodeCache",
                columns: table => new
                {
                    QueryKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeocodeCache", x => x.QueryKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeocodeCache");
        }
    }
}
