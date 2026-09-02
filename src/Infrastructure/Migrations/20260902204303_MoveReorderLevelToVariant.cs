using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveReorderLevelToVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "MedicineVariants",
                type: "int",
                nullable: false,
                defaultValue: 10);

            // Migrate existing medicine-level reorder levels to each of its variants
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

        /// <inheritdoc />
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
