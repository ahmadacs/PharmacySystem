using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{

    public partial class AddPrescriptionShortCode : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "Prescriptions",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE Prescriptions
SET ShortCode = UPPER(LEFT(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', ''), 8))
WHERE ShortCode IS NULL;
");

            migrationBuilder.AlterColumn<string>(
                name: "ShortCode",
                table: "Prescriptions",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ShortCode",
                table: "Prescriptions",
                column: "ShortCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_ShortCode",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "Prescriptions");
        }
    }
}
