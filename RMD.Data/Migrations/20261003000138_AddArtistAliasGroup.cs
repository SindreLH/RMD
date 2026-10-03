using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMD.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistAliasGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AliasGroupId",
                table: "Artists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_AliasGroupId",
                table: "Artists",
                column: "AliasGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artists_AliasGroupId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "AliasGroupId",
                table: "Artists");
        }
    }
}
