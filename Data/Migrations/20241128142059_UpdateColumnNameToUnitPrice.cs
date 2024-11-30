using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumnNameToUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PricePerShare",
                table: "Transaction",
                newName: "UnitPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "Transaction",
                newName: "PricePerShare");
        }
    }
}
