using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentManager.Migrations
{
    /// <inheritdoc />
    public partial class CreateAssetTableAndUpdateTransactionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AspNetUsers_UserId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Transactions");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transaction");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_UserId",
                table: "Transaction",
                newName: "IX_Transaction_UserId");

            migrationBuilder.AddColumn<string>(
                name: "AssetIsinCode",
                table: "Transaction",
                type: "nvarchar(12)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    IsinCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Ticker = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.IsinCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_AssetIsinCode",
                table: "Transaction",
                column: "AssetIsinCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_AspNetUsers_UserId",
                table: "Transaction",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Asset_AssetIsinCode",
                table: "Transaction",
                column: "AssetIsinCode",
                principalTable: "Asset",
                principalColumn: "IsinCode",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_AspNetUsers_UserId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Asset_AssetIsinCode",
                table: "Transaction");

            migrationBuilder.DropTable(
                name: "Asset");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_AssetIsinCode",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "AssetIsinCode",
                table: "Transaction");

            migrationBuilder.RenameTable(
                name: "Transaction",
                newName: "Transactions");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_UserId",
                table: "Transactions",
                newName: "IX_Transactions_UserId");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "Transactions",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AspNetUsers_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
