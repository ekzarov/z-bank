using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfZ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "AccountNumberSequence",
                startValue: 10000001L);

            migrationBuilder.CreateTable(
                name: "AccountAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Actor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccountId = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    SortCode = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    OverdraftLimit = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ActualBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OpenedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LastStatementOn = table.Column<DateOnly>(type: "date", nullable: false),
                    NextStatementOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HasPendingWork = table.Column<bool>(type: "bit", nullable: false),
                    SourceSystem = table.Column<int>(type: "int", nullable: false),
                    SourceIdentifier = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RawSourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.CheckConstraint("CK_Accounts_Currency", "LEN([Currency]) = 3");
                    table.CheckConstraint("CK_Accounts_InterestRate", "[InterestRate] >= 0 AND [InterestRate] <= 9999.99");
                    table.CheckConstraint("CK_Accounts_OverdraftLimit", "[OverdraftLimit] >= 0");
                    table.CheckConstraint("CK_Accounts_Status", "[Status] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_Accounts_Type", "[Type] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_Accounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAuditEntries_AccountId",
                table: "AccountAuditEntries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountAuditEntries_CustomerId",
                table: "AccountAuditEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CustomerId_Status_Id",
                table: "Accounts",
                columns: new[] { "CustomerId", "Status", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAuditEntries");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropSequence(
                name: "AccountNumberSequence");
        }
    }
}
