using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSomePropertiesRangesAndAssetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Assets",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Assets");
        }
    }
}
