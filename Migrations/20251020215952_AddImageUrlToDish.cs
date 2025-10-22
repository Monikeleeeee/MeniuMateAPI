using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeniuMate_API.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToDish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Dishes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Dishes");
        }
    }
}
