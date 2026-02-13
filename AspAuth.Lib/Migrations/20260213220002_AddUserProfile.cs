using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspAuth.Lib.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserProfileAspNetUserId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    AspNetUserId = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.AspNetUserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserProfileAspNetUserId",
                table: "AspNetUsers",
                column: "UserProfileAspNetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserProfile_UserProfileAspNetUserId",
                table: "AspNetUsers",
                column: "UserProfileAspNetUserId",
                principalTable: "UserProfile",
                principalColumn: "AspNetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserProfile_UserProfileAspNetUserId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserProfileAspNetUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserProfileAspNetUserId",
                table: "AspNetUsers");
        }
    }
}
