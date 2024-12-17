using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentManager.Migrations
{
    /// <inheritdoc />
    public partial class CreateAssetMonthlyPricesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetMonthlyPrices",
                columns: table => new
                {
                    AssetIsinCode = table.Column<string>(type: "nvarchar(12)", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMonthlyPrices", x => x.AssetIsinCode);
                    table.ForeignKey(
                        name: "FK_AssetMonthlyPrices_Assets_AssetIsinCode",
                        column: x => x.AssetIsinCode,
                        principalTable: "Assets",
                        principalColumn: "IsinCode",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMonthlyPrices");
        }
    }
}
