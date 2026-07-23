using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfZ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferCorrelationId",
                table: "BookedTransactions",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookedTransactions_TransferCorrelationId",
                table: "BookedTransactions",
                column: "TransferCorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookedTransactions_TransferCorrelationId",
                table: "BookedTransactions");

            migrationBuilder.DropColumn(
                name: "TransferCorrelationId",
                table: "BookedTransactions");
        }
    }
}
