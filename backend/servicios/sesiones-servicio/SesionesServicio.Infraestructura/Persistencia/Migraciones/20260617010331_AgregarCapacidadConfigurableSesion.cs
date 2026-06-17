using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarCapacidadConfigurableSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "maximo_equipos",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "maximo_participantes",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "maximo_participantes_por_equipo",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            // Backfill de sesiones existentes con los valores históricos del ERS
            // (capacidad fija previa a la capacidad configurable). No destructivo:
            // solo rellena las columnas que aplican a cada modo cuando están vacías.
            migrationBuilder.Sql(
                "UPDATE sesiones.\"Sesion\" SET maximo_participantes = 10 " +
                "WHERE tipo_sesion = 'Individual' AND maximo_participantes IS NULL;");

            migrationBuilder.Sql(
                "UPDATE sesiones.\"Sesion\" SET maximo_equipos = 5, maximo_participantes_por_equipo = 2 " +
                "WHERE tipo_sesion = 'Grupal' AND maximo_equipos IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "maximo_equipos",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "maximo_participantes",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "maximo_participantes_por_equipo",
                schema: "sesiones",
                table: "Sesion");
        }
    }
}
