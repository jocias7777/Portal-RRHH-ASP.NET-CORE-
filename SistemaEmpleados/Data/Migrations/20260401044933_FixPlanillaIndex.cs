using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPlanillaIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Planillas_Mes_Anio",
                table: "Planillas");

            migrationBuilder.CreateIndex(
                name: "IX_Planillas_Mes_Anio_Estado",
                table: "Planillas",
                columns: new[] { "Mes", "Anio", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Planillas_Mes_Anio_Estado",
                table: "Planillas");

            migrationBuilder.CreateIndex(
                name: "IX_Planillas_Mes_Anio",
                table: "Planillas",
                columns: new[] { "Mes", "Anio" },
                unique: true);
        }
    }
}
