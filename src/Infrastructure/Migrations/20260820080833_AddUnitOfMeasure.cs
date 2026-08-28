using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseUnitName",
                table: "MedicineVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDivisible",
                table: "MedicineVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PackageUnitName",
                table: "MedicineVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerPackage",
                table: "MedicineVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineVariants_IsActive",
                table: "MedicineVariants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_IsActive",
                table: "Medicines",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_MedicineVariantId",
                table: "MedicineBatches",
                column: "MedicineVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicineVariants_IsActive",
                table: "MedicineVariants");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_IsActive",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_MedicineBatches_MedicineVariantId",
                table: "MedicineBatches");

            migrationBuilder.DropColumn(
                name: "BaseUnitName",
                table: "MedicineVariants");

            migrationBuilder.DropColumn(
                name: "IsDivisible",
                table: "MedicineVariants");

            migrationBuilder.DropColumn(
                name: "PackageUnitName",
                table: "MedicineVariants");

            migrationBuilder.DropColumn(
                name: "UnitsPerPackage",
                table: "MedicineVariants");
        }
    }
}
