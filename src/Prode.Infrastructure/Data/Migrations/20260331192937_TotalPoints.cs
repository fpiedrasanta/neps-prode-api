using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prode.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TotalPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "AspNetUsers");
        }
    }
}
