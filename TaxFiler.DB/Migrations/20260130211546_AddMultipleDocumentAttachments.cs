using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleDocumentAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    AttachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttachedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAttachments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentAttachments_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_DocumentId",
                table: "DocumentAttachments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_TransactionId_DocumentId",
                table: "DocumentAttachments",
                columns: new[] { "TransactionId", "DocumentId" },
                unique: true);

            // Migrate existing single document attachments to the new many-to-many structure
            migrationBuilder.Sql(@"
                INSERT INTO ""DocumentAttachments"" (""TransactionId"", ""DocumentId"", ""AttachedAt"", ""AttachedBy"", ""IsAutomatic"")
                SELECT ""Id"", ""DocumentId"", NOW(), NULL, true
                FROM ""Transactions""
                WHERE ""DocumentId"" IS NOT NULL
            ");

            // Add validation query to ensure data integrity
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    original_count INTEGER;
                    migrated_count INTEGER;
                BEGIN
                    -- Count original single attachments
                    SELECT COUNT(*) INTO original_count
                    FROM ""Transactions""
                    WHERE ""DocumentId"" IS NOT NULL;
                    
                    -- Count migrated attachments
                    SELECT COUNT(*) INTO migrated_count
                    FROM ""DocumentAttachments"";
                    
                    -- Validate migration integrity
                    IF original_count != migrated_count THEN
                        RAISE EXCEPTION 'Migration validation failed: Expected % attachments, found %', original_count, migrated_count;
                    END IF;
                    
                    -- Log successful migration
                    RAISE NOTICE 'Successfully migrated % document attachments', migrated_count;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Before dropping the DocumentAttachments table, restore single document relationships
            // This is a best-effort rollback - only works if each transaction has at most one attachment
            migrationBuilder.Sql(@"
                UPDATE ""Transactions""
                SET ""DocumentId"" = da.""DocumentId""
                FROM ""DocumentAttachments"" da
                WHERE ""Transactions"".""Id"" = da.""TransactionId""
                AND ""Transactions"".""DocumentId"" IS NULL
                AND da.""Id"" = (
                    SELECT MIN(""Id"") 
                    FROM ""DocumentAttachments"" da2 
                    WHERE da2.""TransactionId"" = da.""TransactionId""
                )
            ");

            migrationBuilder.DropTable(
                name: "DocumentAttachments");
        }
    }
}
