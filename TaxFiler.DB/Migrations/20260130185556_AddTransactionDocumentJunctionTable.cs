using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDocumentJunctionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionDocuments",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDocuments", x => new { x.TransactionId, x.DocumentId });
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

            // Data migration: Copy existing transaction-document relationships
            migrationBuilder.Sql(@"
                INSERT INTO ""TransactionDocuments"" (""TransactionId"", ""DocumentId"")
                SELECT ""Id"", ""DocumentId""
                FROM ""Transactions""
                WHERE ""DocumentId"" IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionDocuments");
        }
    }
}
