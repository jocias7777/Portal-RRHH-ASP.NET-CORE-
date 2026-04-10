using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReclutamientoCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AprobadoPor",
                table: "PlazasVacantes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsReemplazo",
                table: "PlazasVacantes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAprobacion",
                table: "PlazasVacantes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuenteReclutamiento",
                table: "PlazasVacantes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoApertura",
                table: "PlazasVacantes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitadoPor",
                table: "PlazasVacantes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalificacionGeneral",
                table: "Candidatos",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EmpleadoId",
                table: "Candidatos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuentePostulacion",
                table: "Candidatos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreReferido",
                table: "Candidatos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Entrevistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidatoId = table.Column<int>(type: "int", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Entrevistador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lugar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Calificacion = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notificado = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrevistas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entrevistas_Candidatos_CandidatoId",
                        column: x => x.CandidatoId,
                        principalTable: "Candidatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialesEstadoPlaza",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlazaVacanteId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CambiadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesEstadoPlaza", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialesEstadoPlaza_PlazasVacantes_PlazaVacanteId",
                        column: x => x.PlazaVacanteId,
                        principalTable: "PlazasVacantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotasCandidato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidatoId = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasCandidato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasCandidato_Candidatos_CandidatoId",
                        column: x => x.CandidatoId,
                        principalTable: "Candidatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_EmpleadoId",
                table: "Candidatos",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entrevistas_CandidatoId",
                table: "Entrevistas",
                column: "CandidatoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesEstadoPlaza_PlazaVacanteId",
                table: "HistorialesEstadoPlaza",
                column: "PlazaVacanteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCandidato_CandidatoId",
                table: "NotasCandidato",
                column: "CandidatoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidatos_Empleados_EmpleadoId",
                table: "Candidatos",
                column: "EmpleadoId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidatos_Empleados_EmpleadoId",
                table: "Candidatos");

            migrationBuilder.DropTable(
                name: "Entrevistas");

            migrationBuilder.DropTable(
                name: "HistorialesEstadoPlaza");

            migrationBuilder.DropTable(
                name: "NotasCandidato");

            migrationBuilder.DropIndex(
                name: "IX_Candidatos_EmpleadoId",
                table: "Candidatos");

            migrationBuilder.DropColumn(
                name: "AprobadoPor",
                table: "PlazasVacantes");

            migrationBuilder.DropColumn(
                name: "EsReemplazo",
                table: "PlazasVacantes");

            migrationBuilder.DropColumn(
                name: "FechaAprobacion",
                table: "PlazasVacantes");

            migrationBuilder.DropColumn(
                name: "FuenteReclutamiento",
                table: "PlazasVacantes");

            migrationBuilder.DropColumn(
                name: "MotivoApertura",
                table: "PlazasVacantes");

            migrationBuilder.DropColumn(
                name: "SolicitadoPor",
                table: "PlazasVacantes");

            migrationBuilder.DropColumn(
                name: "CalificacionGeneral",
                table: "Candidatos");

            migrationBuilder.DropColumn(
                name: "EmpleadoId",
                table: "Candidatos");

            migrationBuilder.DropColumn(
                name: "FuentePostulacion",
                table: "Candidatos");

            migrationBuilder.DropColumn(
                name: "NombreReferido",
                table: "Candidatos");
        }
    }
}
