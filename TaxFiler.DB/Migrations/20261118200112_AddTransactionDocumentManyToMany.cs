using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDocumentManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionDocuments_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDocuments_DocumentId",
                table: "TransactionDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDocuments_TransactionId_Sequence",
                table: "TransactionDocuments",
                columns: new[] { "TransactionId", "Sequence" },
                unique: true);

            // Migrate the existing one-to-one associations into the junction table
            // before dropping the legacy column. Every existing association becomes
            // the primary (sequence 1) document of its transaction.
            migrationBuilder.Sql(
                """
                INSERT INTO "TransactionDocuments" ("TransactionId", "DocumentId", "Sequence", "AssignedAt")
                SELECT "Id", "DocumentId", 1, now()
                FROM "Transactions"
                WHERE "DocumentId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Documents_DocumentId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_DocumentId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "Transactions",
                type: "integer",
                nullable: true);

            // Restore the legacy single-document association from the sequence-1
            // link of each transaction before dropping the junction table.
            migrationBuilder.Sql(
                """
                UPDATE "Transactions" AS t
                SET "DocumentId" = td."DocumentId"
                FROM "TransactionDocuments" AS td
                WHERE td."TransactionId" = t."Id" AND td."Sequence" = 1;
                """);

            migrationBuilder.DropTable(
                name: "TransactionDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_DocumentId",
                table: "Transactions",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Documents_DocumentId",
                table: "Transactions",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id");
        }
    }
}
