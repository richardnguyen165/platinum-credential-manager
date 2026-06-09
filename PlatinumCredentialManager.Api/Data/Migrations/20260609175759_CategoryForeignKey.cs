using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatinumCredentialManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CategoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Credentials",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_CategoryId",
                table: "Credentials",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Credentials_Categories_CategoryId",
                table: "Credentials",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Credentials_Categories_CategoryId",
                table: "Credentials");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Credentials_CategoryId",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Credentials");
        }
    }
}
