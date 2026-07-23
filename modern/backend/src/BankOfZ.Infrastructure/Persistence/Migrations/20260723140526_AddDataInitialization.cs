using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfZ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportReferenceValues",
                columns: table => new
                {
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceIdentifier = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportReferenceValues", x => new { x.Type, x.Code });
                });

            migrationBuilder.CreateTable(
                name: "ImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InputFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StagedManifest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MigrationVersion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    InputCount = table.Column<int>(type: "int", nullable: false),
                    PromotedCount = table.Column<int>(type: "int", nullable: false),
                    RejectedCount = table.Column<int>(type: "int", nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegacyTransactionRuns",
                columns: table => new
                {
                    SourceIdentifier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StoppedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CustomerId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyTransactionRuns", x => x.SourceIdentifier);
                    table.ForeignKey(
                        name: "FK_LegacyTransactionRuns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SetupOperationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MigrationVersion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupOperationAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InputCount = table.Column<int>(type: "int", nullable: false),
                    PromotedCount = table.Column<int>(type: "int", nullable: false),
                    RejectedCount = table.Column<int>(type: "int", nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MigrationVersion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    AlreadyApplied = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportAttempts_ImportRuns_ImportRunId",
                        column: x => x.ImportRunId,
                        principalTable: "ImportRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportStagedRecords",
                columns: table => new
                {
                    ImportRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportStagedRecords", x => new { x.ImportRunId, x.RecordType, x.SourceKey });
                    table.ForeignKey(
                        name: "FK_ImportStagedRecords_ImportRuns_ImportRunId",
                        column: x => x.ImportRunId,
                        principalTable: "ImportRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SourceSystem_SourceIdentifier",
                table: "Customers",
                columns: new[] { "SourceSystem", "SourceIdentifier" },
                unique: true,
                filter: "[SourceIdentifier] IS NOT NULL AND [SourceSystem] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_BookedTransactions_SourceSystem_SourceIdentifier",
                table: "BookedTransactions",
                columns: new[] { "SourceSystem", "SourceIdentifier" },
                unique: true,
                filter: "[SourceIdentifier] IS NOT NULL AND [SourceSystem] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SourceSystem_SourceIdentifier",
                table: "Accounts",
                columns: new[] { "SourceSystem", "SourceIdentifier" },
                unique: true,
                filter: "[SourceIdentifier] IS NOT NULL AND [SourceSystem] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAttempts_ImportRunId_AttemptNumber",
                table: "ImportAttempts",
                columns: new[] { "ImportRunId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRuns_InputFingerprint",
                table: "ImportRuns",
                column: "InputFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTransactionRuns_CustomerId",
                table: "LegacyTransactionRuns",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportAttempts");

            migrationBuilder.DropTable(
                name: "ImportReferenceValues");

            migrationBuilder.DropTable(
                name: "ImportStagedRecords");

            migrationBuilder.DropTable(
                name: "LegacyTransactionRuns");

            migrationBuilder.DropTable(
                name: "SetupOperationAudits");

            migrationBuilder.DropTable(
                name: "ImportRuns");

            migrationBuilder.DropIndex(
                name: "IX_Customers_SourceSystem_SourceIdentifier",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_BookedTransactions_SourceSystem_SourceIdentifier",
                table: "BookedTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_SourceSystem_SourceIdentifier",
                table: "Accounts");
        }
    }
}
