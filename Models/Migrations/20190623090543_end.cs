using Microsoft.EntityFrameworkCore.Migrations;

namespace DbContext.Migrations
{
    public partial class end : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratorParameters",
                table: "Results",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteTens",
                table: "Results",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratorParameters",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "RemoteTens",
                table: "Results");
        }
    }
}
