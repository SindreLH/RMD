using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMD.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitSongArtists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongArtist_Artists_ArtistId",
                table: "SongArtist");

            migrationBuilder.DropForeignKey(
                name: "FK_SongArtist_Songs_SongId",
                table: "SongArtist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SongArtist",
                table: "SongArtist");

            migrationBuilder.RenameTable(
                name: "SongArtist",
                newName: "SongArtists");

            migrationBuilder.RenameIndex(
                name: "IX_SongArtist_ArtistId",
                table: "SongArtists",
                newName: "IX_SongArtists_ArtistId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SongArtists",
                table: "SongArtists",
                columns: new[] { "SongId", "ArtistId", "Role" });

            migrationBuilder.AddForeignKey(
                name: "FK_SongArtists_Artists_ArtistId",
                table: "SongArtists",
                column: "ArtistId",
                principalTable: "Artists",
                principalColumn: "ArtistId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SongArtists_Songs_SongId",
                table: "SongArtists",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "SongId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongArtists_Artists_ArtistId",
                table: "SongArtists");

            migrationBuilder.DropForeignKey(
                name: "FK_SongArtists_Songs_SongId",
                table: "SongArtists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SongArtists",
                table: "SongArtists");

            migrationBuilder.RenameTable(
                name: "SongArtists",
                newName: "SongArtist");

            migrationBuilder.RenameIndex(
                name: "IX_SongArtists_ArtistId",
                table: "SongArtist",
                newName: "IX_SongArtist_ArtistId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SongArtist",
                table: "SongArtist",
                columns: new[] { "SongId", "ArtistId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SongArtist_Artists_ArtistId",
                table: "SongArtist",
                column: "ArtistId",
                principalTable: "Artists",
                principalColumn: "ArtistId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SongArtist_Songs_SongId",
                table: "SongArtist",
                column: "SongId",
                principalTable: "Songs",
                principalColumn: "SongId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
