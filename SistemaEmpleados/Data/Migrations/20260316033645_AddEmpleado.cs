using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PrimerNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SegundoNombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimerApellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SegundoApellido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Genero = table.Column<int>(type: "int", nullable: false),
                    CUI = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NIT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroIGSS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroIRTRA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    TipoContrato = table.Column<int>(type: "int", nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    PuestoId = table.Column<int>(type: "int", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationUserId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleados_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Empleados_Puestos_PuestoId",
                        column: x => x.PuestoId,
                        principalTable: "Puestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Empleados_Usuarios_ApplicationUserId1",
                        column: x => x.ApplicationUserId1,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_ApplicationUserId1",
                table: "Empleados",
                column: "ApplicationUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Codigo",
                table: "Empleados",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_CUI",
                table: "Empleados",
                column: "CUI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_DepartamentoId",
                table: "Empleados",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_PuestoId",
                table: "Empleados",
                column: "PuestoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empleados");
        }
    }
}
