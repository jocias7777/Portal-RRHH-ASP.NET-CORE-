using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEmpleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentosNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Modalidad",
                table: "Documentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroFolio",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlExterna",
                table: "Documentos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "NumeroFolio",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "UrlExterna",
                table: "Documentos");
        }
    }
}
