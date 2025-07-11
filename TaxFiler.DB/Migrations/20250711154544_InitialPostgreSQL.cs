using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalRef = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Orphaned = table.Column<bool>(type: "boolean", nullable: false),
                    Parsed = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: true),
                    Total = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Skonto = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionDocumentMatchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionReceiver = table.Column<string>(type: "text", nullable: false),
                    TransactionCommentPattern = table.Column<string>(type: "text", nullable: false),
                    AmountMatches = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDocumentMatchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Counterparty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransactionDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsOutgoing = table.Column<bool>(type: "boolean", nullable: false),
                    IsIncomeTaxRelevant = table.Column<bool>(type: "boolean", nullable: false),
                    IsSalesTaxRelevant = table.Column<bool>(type: "boolean", nullable: false),
                    TaxMonth = table.Column<int>(type: "integer", nullable: true),
                    TaxYear = table.Column<int>(type: "integer", nullable: true),
                    DocumentId = table.Column<int>(type: "integer", nullable: true),
                    SenderReceiver = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_DocumentId",
                table: "Transactions",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionDocumentMatchers");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
