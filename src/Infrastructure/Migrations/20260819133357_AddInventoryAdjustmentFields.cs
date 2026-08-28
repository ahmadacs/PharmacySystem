using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAdjustmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdjustedBy",
                table: "InventoryAdjustments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityAfter",
                table: "InventoryAdjustments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityBefore",
                table: "InventoryAdjustments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustedBy",
                table: "InventoryAdjustments");

            migrationBuilder.DropColumn(
                name: "QuantityAfter",
                table: "InventoryAdjustments");

            migrationBuilder.DropColumn(
                name: "QuantityBefore",
                table: "InventoryAdjustments");
        }
    }
}
