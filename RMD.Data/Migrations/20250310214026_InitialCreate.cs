using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RMD.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    ArtistId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacebookUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoundcloudUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscogsUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.ArtistId);
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    SongId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RemixArtist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Length = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Genre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtendedMix = table.Column<bool>(type: "bit", nullable: false),
                    RadioMix = table.Column<bool>(type: "bit", nullable: false),
                    Played = table.Column<bool>(type: "bit", nullable: false),
                    PlayedInEp = table.Column<int>(type: "int", nullable: true),
                    Stored = table.Column<bool>(type: "bit", nullable: false),
                    Wanted = table.Column<bool>(type: "bit", nullable: false),
                    WantedSongUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Favorite = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.SongId);
                });

            migrationBuilder.InsertData(
                table: "Artists",
                columns: new[] { "ArtistId", "DiscogsUrl", "FacebookUrl", "Name", "Nationality", "ProfilePicUrl", "SoundcloudUrl" },
                values: new object[,]
                {
                    { 1, "https://www.test.com/", "https://www.facebook.com/RejackHS", "Rejack", "🇳🇴 Norge", "https://www.test.com/", "https://www.soundcloud.com/RejackHS" },
                    { 2, "https://www.test.com/", "https://www.facebook.com/RejackHS", "Rejack 2", "🇳🇴 Norge", "https://www.test.com/", "https://www.soundcloud.com/RejackHS" }
                });

            migrationBuilder.InsertData(
                table: "Songs",
                columns: new[] { "SongId", "Artist", "ExtendedMix", "Favorite", "Genre", "Length", "Played", "PlayedInEp", "RadioMix", "RemixArtist", "Stored", "Title", "Wanted", "WantedSongUrl" },
                values: new object[] { 1, "Artist", true, true, "Hands Up", "02:23", true, 18, false, "Test", true, "Hands Up Track (Test Remix)", false, "https://www.soundcloud.com/RejackHS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropTable(
                name: "Songs");
        }
    }
}
