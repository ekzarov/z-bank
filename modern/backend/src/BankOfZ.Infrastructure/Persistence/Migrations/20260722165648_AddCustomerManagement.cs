using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfZ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "CustomerNumberSequence",
                startValue: 1000000002L);

            migrationBuilder.Sql("UPDATE AspNetUsers SET CustomerId = NULL WHERE CustomerId IS NOT NULL");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "AspNetUsers",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Actor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    SortCode = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(129)", maxLength: 129, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreditScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreditReviewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceSystem = table.Column<int>(type: "int", nullable: false),
                    SourceIdentifier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CustomerId",
                table: "AspNetUsers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuditEntries_CustomerId",
                table: "CustomerAuditEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedName_Active",
                table: "Customers",
                column: "NormalizedName",
                filter: "[Status] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Customers_CustomerId",
                table: "AspNetUsers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Customers_CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CustomerAuditEntries");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropSequence(
                name: "CustomerNumberSequence");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "AspNetUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldUnicode: false,
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
