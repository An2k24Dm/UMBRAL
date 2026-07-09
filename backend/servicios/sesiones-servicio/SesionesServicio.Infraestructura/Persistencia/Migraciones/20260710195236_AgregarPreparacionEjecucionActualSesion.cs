using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPreparacionEjecucionActualSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ejecucion_actual_duracion_preparacion_segundos",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ejecucion_actual_fase",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ejecucion_actual_orden_etapa",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ejecucion_actual_orden_mision",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ejecucion_actual_duracion_preparacion_segundos",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_fase",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_orden_etapa",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_orden_mision",
                schema: "sesiones",
                table: "Sesion");
        }
    }
}
