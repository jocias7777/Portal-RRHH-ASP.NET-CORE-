using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRelacionEmpleadoUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Usuarios_ApplicationUserId1",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_ApplicationUserId1",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Empleados");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Empleados",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_ApplicationUserId",
                table: "Empleados",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Usuarios_ApplicationUserId",
                table: "Empleados",
                column: "ApplicationUserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Usuarios_ApplicationUserId",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_ApplicationUserId",
                table: "Empleados");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Empleados",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId1",
                table: "Empleados",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_ApplicationUserId1",
                table: "Empleados",
                column: "ApplicationUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Usuarios_ApplicationUserId1",
                table: "Empleados",
                column: "ApplicationUserId1",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }
    }
}
