using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefillIntervalToPrescriptionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "LastDispensedAt",
                table: "PrescriptionItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefillIntervalDays",
                table: "PrescriptionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDispensedAt",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "RefillIntervalDays",
                table: "PrescriptionItems");
        }
    }
}
