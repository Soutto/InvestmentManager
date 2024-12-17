using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentManager.Migrations
{
    /// <inheritdoc />
    public partial class CreateCompositeKeyInAssetMonthlyPriceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssetMonthlyPrices",
                table: "AssetMonthlyPrices");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssetMonthlyPrices",
                table: "AssetMonthlyPrices",
                columns: new[] { "AssetIsinCode", "Year", "Month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssetMonthlyPrices",
                table: "AssetMonthlyPrices");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssetMonthlyPrices",
                table: "AssetMonthlyPrices",
                column: "AssetIsinCode");
        }
    }
}
