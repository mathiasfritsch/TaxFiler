using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class change : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionDocumentMatchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransactionReceiver = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionCommentPattern = table.Column<string>(type: "TEXT", nullable: false),
                    AmountMatches = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDocumentMatchers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionDocumentMatchers");
        }
    }
}
