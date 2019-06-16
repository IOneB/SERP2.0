using Microsoft.EntityFrameworkCore.Migrations;

namespace DbContext.Migrations
{
    public partial class updatedresult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Results_Users_UserID",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Results_UserID",
                table: "Results");

            migrationBuilder.AddColumn<string>(
                name: "L",
                table: "Results",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "U1",
                table: "Results",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "U2",
                table: "Results",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "L",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "U1",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "U2",
                table: "Results");

            migrationBuilder.CreateIndex(
                name: "IX_Results_UserID",
                table: "Results",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Users_UserID",
                table: "Results",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
