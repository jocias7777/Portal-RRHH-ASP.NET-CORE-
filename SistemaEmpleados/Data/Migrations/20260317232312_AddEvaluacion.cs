using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    EvaluadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoEvaluacion = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PuntajeTotal = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Comentarios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanMejora = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evaluaciones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evaluaciones_Usuarios_EvaluadorId",
                        column: x => x.EvaluadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KPIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Peso = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PuestoId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPIs_Puestos_PuestoId",
                        column: x => x.PuestoId,
                        principalTable: "Puestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosKPI",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluacionId = table.Column<int>(type: "int", nullable: false),
                    KPIId = table.Column<int>(type: "int", nullable: false),
                    Calificacion = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PuntajePonderado = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosKPI", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadosKPI_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultadosKPI_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_EmpleadoId",
                table: "Evaluaciones",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_EvaluadorId",
                table: "Evaluaciones",
                column: "EvaluadorId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_PuestoId",
                table: "KPIs",
                column: "PuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosKPI_EvaluacionId",
                table: "ResultadosKPI",
                column: "EvaluacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosKPI_KPIId",
                table: "ResultadosKPI",
                column: "KPIId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultadosKPI");

            migrationBuilder.DropTable(
                name: "Evaluaciones");

            migrationBuilder.DropTable(
                name: "KPIs");
        }
    }
}
