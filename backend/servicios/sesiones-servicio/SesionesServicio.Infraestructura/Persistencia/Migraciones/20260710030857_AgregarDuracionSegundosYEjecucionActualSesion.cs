using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarDuracionSegundosYEjecucionActualSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duracion_segundos_limite",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE sesiones."Sesion"
                SET duracion_segundos_limite = duracion_minutos_limite * 60
                WHERE duracion_minutos_limite IS NOT NULL;
                """);

            migrationBuilder.AddColumn<int>(
                name: "ejecucion_actual_orden_global",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ejecucion_actual_duracion_pausas_acumulada_ms",
                schema: "sesiones",
                table: "Sesion",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ejecucion_actual_duracion_segundos",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ejecucion_actual_etapa_id",
                schema: "sesiones",
                table: "Sesion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ejecucion_actual_fecha_inicio_pausa_utc",
                schema: "sesiones",
                table: "Sesion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ejecucion_actual_fecha_inicio_utc",
                schema: "sesiones",
                table: "Sesion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ejecucion_actual_mision_id",
                schema: "sesiones",
                table: "Sesion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ejecucion_actual_modo_de_juego_id",
                schema: "sesiones",
                table: "Sesion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ejecucion_actual_tipo_etapa",
                schema: "sesiones",
                table: "Sesion",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.DropColumn(
                name: "duracion_minutos_limite",
                schema: "sesiones",
                table: "Sesion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duracion_minutos_limite",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE sesiones."Sesion"
                SET duracion_minutos_limite = CEIL(duracion_segundos_limite / 60.0)::integer
                WHERE duracion_segundos_limite IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "duracion_segundos_limite",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_duracion_pausas_acumulada_ms",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_duracion_segundos",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_etapa_id",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_fecha_inicio_pausa_utc",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_fecha_inicio_utc",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_mision_id",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_modo_de_juego_id",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_tipo_etapa",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "ejecucion_actual_orden_global",
                schema: "sesiones",
                table: "Sesion");
        }
    }
}
