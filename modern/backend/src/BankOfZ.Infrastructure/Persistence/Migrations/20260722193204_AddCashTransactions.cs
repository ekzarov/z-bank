using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfZ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastTransactionReference",
                table: "Accounts",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookedTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    AccountId = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ResultingActualBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResultingAvailableBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    SourceSystem = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookedTransactions", x => x.Id);
                    table.CheckConstraint("CK_BookedTransactions_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_BookedTransactions_Currency", "LEN([Currency]) = 3");
                    table.CheckConstraint("CK_BookedTransactions_Direction", "[Direction] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_BookedTransactions_SourceSystem", "[SourceSystem] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_BookedTransactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookedTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookedTransactions_AccountId_CreatedAt",
                table: "BookedTransactions",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookedTransactions_AccountId_IdempotencyKey",
                table: "BookedTransactions",
                columns: new[] { "AccountId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookedTransactions_CustomerId",
                table: "BookedTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookedTransactions_Reference",
                table: "BookedTransactions",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookedTransactions");

            migrationBuilder.DropColumn(
                name: "LastTransactionReference",
                table: "Accounts");
        }
    }
}
