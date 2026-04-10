using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNomina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Planillas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Periodo = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    GeneradoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalDevengado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalNeto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planillas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPlanilla",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanillaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HorasExtraMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Bonificacion250 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtrosBonos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDevengado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CuotaIGSS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ISR = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtrasDeducciones = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalarioNeto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPlanilla", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesPlanilla_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesPlanilla_Planillas_PlanillaId",
                        column: x => x.PlanillaId,
                        principalTable: "Planillas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPlanilla_EmpleadoId",
                table: "DetallesPlanilla",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPlanilla_PlanillaId",
                table: "DetallesPlanilla",
                column: "PlanillaId");

            migrationBuilder.CreateIndex(
                name: "IX_Planillas_Mes_Anio",
                table: "Planillas",
                columns: new[] { "Mes", "Anio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesPlanilla");

            migrationBuilder.DropTable(
                name: "Planillas");
        }
    }
}
