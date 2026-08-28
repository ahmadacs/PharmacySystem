using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoryToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add the new CategoryEnum column with default value
            migrationBuilder.AddColumn<int>(
                name: "CategoryEnum",
                table: "Medicines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Migrate data from CategoryId to CategoryEnum based on Category.Name
            migrationBuilder.Sql(@"
                UPDATE Medicines
                SET CategoryEnum = CASE 
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Analgesics') THEN 1
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Antibiotics') THEN 2
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Antipyretics') THEN 3
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Anticoagulants') THEN 4
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Antihistamines') THEN 5
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Cardiovascular') THEN 6
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Diabetic') THEN 7
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Antidiabetics') THEN 8
                    WHEN CategoryId IN (SELECT Id FROM Categories WHERE Name = 'Respiratory') THEN 9
                    ELSE 10
                END;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_Categories_CategoryId",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Medicines");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_CategoryEnum",
                table: "Medicines",
                column: "CategoryEnum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medicines_CategoryEnum",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "CategoryEnum",
                table: "Medicines");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Medicines",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_Categories_CategoryId",
                table: "Medicines",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
