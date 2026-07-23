using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfZ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatementAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountId = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Diagnostics = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatementSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataAsOf = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(144)", maxLength: 144, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SortCode = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    AccountType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    OverdraftLimit = table.Column<int>(type: "int", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementSnapshots", x => x.Id);
                    table.CheckConstraint("CK_StatementSnapshots_Month", "[Month] BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "StatementTransactionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    BookedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementTransactionSnapshots", x => x.Id);
                    table.CheckConstraint("CK_StatementTransactionSnapshots_Direction", "[Direction] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_StatementTransactionSnapshots_StatementSnapshots_StatementId",
                        column: x => x.StatementId,
                        principalTable: "StatementSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatementAuditEntries_AccountId_OccurredAt",
                table: "StatementAuditEntries",
                columns: new[] { "AccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementSnapshots_AccountId_Year_Month_DataVersion",
                table: "StatementSnapshots",
                columns: new[] { "AccountId", "Year", "Month", "DataVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatementTransactionSnapshots_StatementId_Sequence",
                table: "StatementTransactionSnapshots",
                columns: new[] { "StatementId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatementAuditEntries");

            migrationBuilder.DropTable(
                name: "StatementTransactionSnapshots");

            migrationBuilder.DropTable(
                name: "StatementSnapshots");
        }
    }
}
