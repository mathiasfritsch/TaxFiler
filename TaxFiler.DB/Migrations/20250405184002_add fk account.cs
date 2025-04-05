using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class addfkaccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        { 
            
            migrationBuilder.Sql("Insert into Accounts (Name) values ('GLS')");
            migrationBuilder.Sql("Insert into Accounts (Name) values ('Credit Card')");
            migrationBuilder.Sql("Update Transactions set AccountId = 1");
            
            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");
            
            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_AccountId",
                table: "Transactions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
