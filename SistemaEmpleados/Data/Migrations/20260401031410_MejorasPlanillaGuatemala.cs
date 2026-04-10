using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class MejorasPlanillaGuatemala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Aguinaldo",
                table: "DetallesPlanilla",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Bono14",
                table: "DetallesPlanilla",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CuotaIGSSPatronal",
                table: "DetallesPlanilla",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPrestamo",
                table: "DetallesPlanilla",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aguinaldo",
                table: "DetallesPlanilla");

            migrationBuilder.DropColumn(
                name: "Bono14",
                table: "DetallesPlanilla");

            migrationBuilder.DropColumn(
                name: "CuotaIGSSPatronal",
                table: "DetallesPlanilla");

            migrationBuilder.DropColumn(
                name: "DescuentoPrestamo",
                table: "DetallesPlanilla");
        }
    }
}
