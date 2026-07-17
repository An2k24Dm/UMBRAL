using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AlinearPenalizacionesAplicadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PenalizacionSesion_evento_id",
                schema: "sesiones",
                table: "PenalizacionSesion");

            migrationBuilder.DropIndex(
                name: "IX_PenalizacionSesion_sesion_id",
                schema: "sesiones",
                table: "PenalizacionSesion");

            migrationBuilder.DropIndex(
                name: "IX_PenalizacionSesion_sesion_id_equipo_id",
                schema: "sesiones",
                table: "PenalizacionSesion");

            migrationBuilder.DropIndex(
                name: "IX_PenalizacionSesion_sesion_id_participante_sesion_id",
                schema: "sesiones",
                table: "PenalizacionSesion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PenalizacionSesion",
                schema: "sesiones",
                table: "PenalizacionSesion");

            migrationBuilder.RenameTable(
                name: "PenalizacionSesion",
                schema: "sesiones",
                newName: "penalizaciones_aplicadas",
                newSchema: "sesiones");

            migrationBuilder.RenameColumn(
                name: "puntos",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                newName: "puntos_descontados");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "sesiones",
                table: "penalizaciones_aplicadas");

            migrationBuilder.DropColumn(
                name: "estado_procesamiento",
                schema: "sesiones",
                table: "penalizaciones_aplicadas");

            migrationBuilder.DropColumn(
                name: "procesada_en_utc",
                schema: "sesiones",
                table: "penalizaciones_aplicadas");

            migrationBuilder.DropColumn(
                name: "puntaje_resultante",
                schema: "sesiones",
                table: "penalizaciones_aplicadas");

            migrationBuilder.AddPrimaryKey(
                name: "PK_penalizaciones_aplicadas",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                column: "evento_id");

            migrationBuilder.CreateTable(
                name: "resultados_ranking_procesados",
                schema: "sesiones",
                columns: table => new
                {
                    evento_id_origen = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_resultado = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    procesado_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultados_ranking_procesados", x => new { x.evento_id_origen, x.tipo_resultado });
                });

            migrationBuilder.CreateIndex(
                name: "IX_penalizaciones_aplicadas_aplicada_en_utc",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                column: "aplicada_en_utc");

            migrationBuilder.CreateIndex(
                name: "IX_penalizaciones_aplicadas_equipo_id",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_penalizaciones_aplicadas_operador_identidad_id",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                column: "operador_identidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_penalizaciones_aplicadas_participante_identidad_id",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                column: "participante_identidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_penalizaciones_aplicadas_sesion_id",
                schema: "sesiones",
                table: "penalizaciones_aplicadas",
                column: "sesion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "penalizaciones_aplicadas",
                schema: "sesiones");

            migrationBuilder.DropTable(
                name: "resultados_ranking_procesados",
                schema: "sesiones");

            migrationBuilder.CreateTable(
                name: "PenalizacionSesion",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aplicada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado_procesamiento = table.Column<int>(type: "integer", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    operador_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participante_sesion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    procesada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    puntaje_resultante = table.Column<long>(type: "bigint", nullable: true),
                    puntos = table.Column<int>(type: "integer", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_objetivo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenalizacionSesion", x => x.id);
                    table.CheckConstraint("ck_penalizacion_motivo_no_vacio", "length(btrim(motivo)) > 0");
                    table.CheckConstraint("ck_penalizacion_objetivo_coherente", "(tipo_objetivo = 0 AND participante_sesion_id IS NOT NULL AND participante_identidad_id IS NOT NULL AND equipo_id IS NULL) OR (tipo_objetivo = 1 AND equipo_id IS NOT NULL AND participante_sesion_id IS NULL AND participante_identidad_id IS NULL)");
                    table.CheckConstraint("ck_penalizacion_puntos_rango", "puntos BETWEEN 1 AND 100");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PenalizacionSesion_evento_id",
                schema: "sesiones",
                table: "PenalizacionSesion",
                column: "evento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PenalizacionSesion_sesion_id",
                schema: "sesiones",
                table: "PenalizacionSesion",
                column: "sesion_id");

            migrationBuilder.CreateIndex(
                name: "IX_PenalizacionSesion_sesion_id_equipo_id",
                schema: "sesiones",
                table: "PenalizacionSesion",
                columns: new[] { "sesion_id", "equipo_id" });

            migrationBuilder.CreateIndex(
                name: "IX_PenalizacionSesion_sesion_id_participante_sesion_id",
                schema: "sesiones",
                table: "PenalizacionSesion",
                columns: new[] { "sesion_id", "participante_sesion_id" });
        }
    }
}
