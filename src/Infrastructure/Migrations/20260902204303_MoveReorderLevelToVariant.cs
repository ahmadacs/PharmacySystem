using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{

    public partial class MoveReorderLevelToVariant : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "MedicineVariants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.Sql(@"
                UPDATE mv
                SET mv.ReorderLevel = m.ReorderLevel
                FROM MedicineVariants mv
                INNER JOIN Medicines m ON mv.MedicineId = m.Id
            ");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "Medicines");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "MedicineVariants");

            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "Medicines",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
