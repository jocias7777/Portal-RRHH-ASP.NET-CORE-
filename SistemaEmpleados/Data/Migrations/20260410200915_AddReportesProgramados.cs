using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportesProgramados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportesProgramados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoReporte = table.Column<int>(type: "int", nullable: false),
                    Frecuencia = table.Column<int>(type: "int", nullable: false),
                    DepartamentoId = table.Column<int>(type: "int", nullable: true),
                    EmailDestino = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailsCC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoraEnvio = table.Column<TimeSpan>(type: "time", nullable: true),
                    DiaSemana = table.Column<int>(type: "int", nullable: true),
                    DiaMes = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UltimoEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProximoEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IncluirExcel = table.Column<bool>(type: "bit", nullable: false),
                    IncluirPDF = table.Column<bool>(type: "bit", nullable: false),
                    EnviarAlertas = table.Column<bool>(type: "bit", nullable: false),
                    UltimoError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesProgramados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportesProgramados_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportesProgramados_DepartamentoId",
                table: "ReportesProgramados",
                column: "DepartamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportesProgramados");
        }
    }
}
