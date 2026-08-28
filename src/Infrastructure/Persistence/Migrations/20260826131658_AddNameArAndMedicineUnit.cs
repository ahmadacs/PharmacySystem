using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNameArAndMedicineUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants");

            // 1. Add new temp column for enum
            migrationBuilder.AddColumn<int>(
                name: "UnitEnum",
                table: "MedicineVariants",
                type: "int",
                nullable: false,
                defaultValue: 99); // Other

            // 2. Migrate string data to enum values
            migrationBuilder.Sql(@"
                UPDATE [MedicineVariants]
                SET [UnitEnum] = CASE 
                    WHEN [Unit] = 'mg' THEN 1
                    WHEN [Unit] = 'ml' THEN 2
                    WHEN [Unit] = 'g' THEN 3
                    WHEN [Unit] = 'Tablet' THEN 4
                    WHEN [Unit] = 'Capsule' THEN 5
                    WHEN [Unit] = 'Drop' THEN 6
                    WHEN [Unit] = 'Vial' THEN 7
                    WHEN [Unit] = 'Ampoule' THEN 8
                    WHEN [Unit] = 'Sachet' THEN 9
                    WHEN [Unit] = 'Patch' THEN 10
                    WHEN [Unit] = 'Spray' THEN 11
                    WHEN [Unit] = 'Suppository' THEN 12
                    WHEN [Unit] = 'IU' THEN 13
                    WHEN [Unit] = '%' THEN 14
                    ELSE 99 -- Other
                END
            ");

            // 3. Drop old string column
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MedicineVariants");

            // 4. Rename temp column to Unit
            migrationBuilder.RenameColumn(
                name: "UnitEnum",
                table: "MedicineVariants",
                newName: "Unit");

            // 5. Make Strength non-nullable (all existing rows should have values)
            migrationBuilder.AlterColumn<decimal>(
                name: "Strength",
                table: "MedicineVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Medicines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "GenericNames",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants",
                columns: new[] { "MedicineId", "Form", "Unit", "Strength" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants");

            // Reverse: Add temp string column
            migrationBuilder.AddColumn<string>(
                name: "UnitString",
                table: "MedicineVariants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");

            // Migrate enum back to string
            migrationBuilder.Sql(@"
                UPDATE [MedicineVariants]
                SET [UnitString] = CASE 
                    WHEN [Unit] = 1 THEN 'mg'
                    WHEN [Unit] = 2 THEN 'ml'
                    WHEN [Unit] = 3 THEN 'g'
                    WHEN [Unit] = 4 THEN 'Tablet'
                    WHEN [Unit] = 5 THEN 'Capsule'
                    WHEN [Unit] = 6 THEN 'Drop'
                    WHEN [Unit] = 7 THEN 'Vial'
                    WHEN [Unit] = 8 THEN 'Ampoule'
                    WHEN [Unit] = 9 THEN 'Sachet'
                    WHEN [Unit] = 10 THEN 'Patch'
                    WHEN [Unit] = 11 THEN 'Spray'
                    WHEN [Unit] = 12 THEN 'Suppository'
                    WHEN [Unit] = 13 THEN 'IU'
                    WHEN [Unit] = 14 THEN '%'
                    ELSE 'Other'
                END
            ");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MedicineVariants");

            migrationBuilder.RenameColumn(
                name: "UnitString",
                table: "MedicineVariants",
                newName: "Unit");

            migrationBuilder.AlterColumn<decimal>(
                name: "Strength",
                table: "MedicineVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "GenericNames");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineVariants_MedicineId_Form_Unit_Strength",
                table: "MedicineVariants",
                columns: new[] { "MedicineId", "Form", "Unit", "Strength" },
                unique: true,
                filter: "[Strength] IS NOT NULL");
        }
    }
}