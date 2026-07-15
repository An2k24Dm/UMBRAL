using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarTipoYContrasenaEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "capacidad_maxima",
                schema: "sesiones",
                table: "Equipo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "contrasena_hash",
                schema: "sesiones",
                table: "Equipo",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tipo_equipo",
                schema: "sesiones",
                table: "Equipo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill técnico (no regla de negocio) para equipos previos a HU40:
            // la capacidad por equipo se deriva de la sesión grupal a la que
            // pertenecen; si la sesión no la tuviera, se usa el mínimo técnico 2.
            // tipo_equipo queda en 0 (Publico) y contrasena_hash en NULL.
            migrationBuilder.Sql(
                "UPDATE sesiones.\"Equipo\" e " +
                "SET capacidad_maxima = COALESCE(s.maximo_participantes_por_equipo, 2) " +
                "FROM sesiones.\"Sesion\" s " +
                "WHERE e.sesion_id = s.id AND e.capacidad_maxima = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capacidad_maxima",
                schema: "sesiones",
                table: "Equipo");

            migrationBuilder.DropColumn(
                name: "contrasena_hash",
                schema: "sesiones",
                table: "Equipo");

            migrationBuilder.DropColumn(
                name: "tipo_equipo",
                schema: "sesiones",
                table: "Equipo");
        }
    }
}
