using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propyka.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShowExactLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowExactLocation",
                table: "Properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowExactLocation",
                table: "Properties");
        }
    }
}
