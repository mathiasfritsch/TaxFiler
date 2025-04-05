using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class addaccoun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            // migrationBuilder.CreateIndex(
            //     name: "IX_Transactions_AccountId",
            //     table: "Transactions",
            //     column: "AccountId");
            //
            // migrationBuilder.AddForeignKey(
            //     name: "FK_Transactions_Accounts_AccountId",
            //     table: "Transactions",
            //     column: "AccountId",
            //     principalTable: "Accounts",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_AccountId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Transactions");
        }
    }
}
