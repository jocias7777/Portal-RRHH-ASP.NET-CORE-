using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregaHistorialSalarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialesSalario_Empleados_EmpleadoId",
                table: "HistorialesSalario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistorialesSalario",
                table: "HistorialesSalario");

            migrationBuilder.DropColumn(
                name: "AutorizadoPor",
                table: "HistorialesSalario");

            migrationBuilder.RenameTable(
                name: "HistorialesSalario",
                newName: "HistorialSalario");

            migrationBuilder.RenameColumn(
                name: "Observacion",
                table: "HistorialSalario",
                newName: "CambiadoPor");

            migrationBuilder.RenameColumn(
                name: "FechaEfectiva",
                table: "HistorialSalario",
                newName: "FechaCambio");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialesSalario_EmpleadoId",
                table: "HistorialSalario",
                newName: "IX_HistorialSalario_EmpleadoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistorialSalario",
                table: "HistorialSalario",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSalario_Empleados_EmpleadoId",
                table: "HistorialSalario",
                column: "EmpleadoId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSalario_Empleados_EmpleadoId",
                table: "HistorialSalario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistorialSalario",
                table: "HistorialSalario");

            migrationBuilder.RenameTable(
                name: "HistorialSalario",
                newName: "HistorialesSalario");

            migrationBuilder.RenameColumn(
                name: "FechaCambio",
                table: "HistorialesSalario",
                newName: "FechaEfectiva");

            migrationBuilder.RenameColumn(
                name: "CambiadoPor",
                table: "HistorialesSalario",
                newName: "Observacion");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialSalario_EmpleadoId",
                table: "HistorialesSalario",
                newName: "IX_HistorialesSalario_EmpleadoId");

            migrationBuilder.AddColumn<string>(
                name: "AutorizadoPor",
                table: "HistorialesSalario",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistorialesSalario",
                table: "HistorialesSalario",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialesSalario_Empleados_EmpleadoId",
                table: "HistorialesSalario",
                column: "EmpleadoId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
