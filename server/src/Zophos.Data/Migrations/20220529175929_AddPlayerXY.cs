using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zophos.Data.Migrations
{
    public partial class AddPlayerXY : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "X",
                table: "Players",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Y",
                table: "Players",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "X",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "Players");
        }
    }
}
