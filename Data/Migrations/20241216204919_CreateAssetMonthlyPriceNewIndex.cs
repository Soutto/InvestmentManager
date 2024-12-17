using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentManager.Migrations
{
    /// <inheritdoc />
    public partial class CreateAssetMonthlyPriceNewIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "AssetIsinCode_Year_Month_Index",
                table: "AssetMonthlyPrices",
                columns: new[] { "AssetIsinCode", "Year", "Month" })
                .Annotation("SqlServer:Include", new[] { "Price" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "AssetIsinCode_Year_Month_Index",
                table: "AssetMonthlyPrices");
        }
    }
}
