using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetTrackerApp.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_CategoryId_TransactionDate",
                table: "Transactions",
                columns: new[] { "AccountId", "CategoryId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_TransactionDate",
                table: "Transactions",
                columns: new[] { "AccountId", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_CategoryId_TransactionDate",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_TransactionDate",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");
        }
    }
}
