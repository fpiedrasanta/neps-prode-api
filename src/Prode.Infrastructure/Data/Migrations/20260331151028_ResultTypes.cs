using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prode.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResultTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_ResultType_ResultTypeId",
                table: "Predictions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResultType",
                table: "ResultType");

            migrationBuilder.RenameTable(
                name: "ResultType",
                newName: "ResultTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResultTypes",
                table: "ResultTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_ResultTypes_ResultTypeId",
                table: "Predictions",
                column: "ResultTypeId",
                principalTable: "ResultTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_ResultTypes_ResultTypeId",
                table: "Predictions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResultTypes",
                table: "ResultTypes");

            migrationBuilder.RenameTable(
                name: "ResultTypes",
                newName: "ResultType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResultType",
                table: "ResultType",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_ResultType_ResultTypeId",
                table: "Predictions",
                column: "ResultTypeId",
                principalTable: "ResultType",
                principalColumn: "Id");
        }
    }
}
