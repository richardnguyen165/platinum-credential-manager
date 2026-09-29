using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatinumCredentialManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueKeycloakId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakId",
                table: "Users",
                column: "KeycloakId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_KeycloakId",
                table: "Users");
        }
    }
}
