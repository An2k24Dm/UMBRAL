using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarTipoPistaGpsEnSesiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tipo",
                schema: "sesiones",
                table: "PistaLiberada",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "latitud",
                schema: "sesiones",
                table: "PistaLiberada",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitud",
                schema: "sesiones",
                table: "PistaLiberada",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "sesiones",
                table: "PistaLiberada");

            migrationBuilder.DropColumn(
                name: "latitud",
                schema: "sesiones",
                table: "PistaLiberada");

            migrationBuilder.DropColumn(
                name: "longitud",
                schema: "sesiones",
                table: "PistaLiberada");
        }
    }
}
