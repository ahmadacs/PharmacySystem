using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeVariantStrengthToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants");

            migrationBuilder.AlterColumn<decimal>(
                name: "Strength",
                table: "MedicineVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants",
                columns: new[] { "MedicineId", "Form", "Unit", "Strength" },
                unique: true,
                filter: "[Strength] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants");

            migrationBuilder.AlterColumn<string>(
                name: "Strength",
                table: "MedicineVariants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants",
                columns: new[] { "MedicineId", "Form", "Unit", "Strength" },
                unique: true);
        }
    }
}
