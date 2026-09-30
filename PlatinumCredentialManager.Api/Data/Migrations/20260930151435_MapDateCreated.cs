using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatinumCredentialManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MapDateCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateCreated",
                table: "Credentials",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("UPDATE Credentials SET DateCreated = DateLastUpdated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "Credentials");
        }
    }
}
