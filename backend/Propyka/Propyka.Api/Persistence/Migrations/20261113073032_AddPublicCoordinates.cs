using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propyka.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PublicLatitude",
                table: "PropertyAddresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PublicLongitude",
                table: "PropertyAddresses",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAddresses_PublicLatitude_PublicLongitude",
                table: "PropertyAddresses",
                columns: new[] { "PublicLatitude", "PublicLongitude" });

            // Fill the public point for listings that already have a pin. Same rule as
            // PropertyAddress.RefreshPublicLocation: exact if the owner allows it, otherwise
            // rounded to 3 decimal places. Postgres rounds numeric halves away from zero,
            // like MidpointRounding.AwayFromZero in the C# version.
            migrationBuilder.Sql("""
                UPDATE "PropertyAddresses" AS a
                SET "PublicLatitude"  = CASE WHEN p."ShowExactLocation" THEN a."Latitude"
                                             ELSE round(a."Latitude"::numeric, 3)::double precision END,
                    "PublicLongitude" = CASE WHEN p."ShowExactLocation" THEN a."Longitude"
                                             ELSE round(a."Longitude"::numeric, 3)::double precision END
                FROM "Properties" AS p
                WHERE p."Id" = a."PropertyId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyAddresses_PublicLatitude_PublicLongitude",
                table: "PropertyAddresses");

            migrationBuilder.DropColumn(
                name: "PublicLatitude",
                table: "PropertyAddresses");

            migrationBuilder.DropColumn(
                name: "PublicLongitude",
                table: "PropertyAddresses");
        }
    }
}
