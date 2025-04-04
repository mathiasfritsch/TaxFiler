using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxMonth",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TaxYear",
                table: "Documents");

            migrationBuilder.AlterColumn<int>(
                name: "TaxYear",
                table: "Transactions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "TaxMonth",
                table: "Transactions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Bookings",
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

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AccountId",
                table: "Bookings",
                column: "AccountId");
            
            migrationBuilder.Sql("Insert Into Account( Name ) Values('GLS')");
            migrationBuilder.Sql("Insert Into Account( Name ) Values('CC-Card')");
            
            migrationBuilder.Sql("Update Transactions set AccountId = 1");
                
            migrationBuilder.AddForeignKey(
            name: "FK_Bookings_Accounts_AccountId",
            table: "Bookings",
            column: "AccountId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Accounts_AccountId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AccountId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "TaxYear",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TaxMonth",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxMonth",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TaxYear",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
