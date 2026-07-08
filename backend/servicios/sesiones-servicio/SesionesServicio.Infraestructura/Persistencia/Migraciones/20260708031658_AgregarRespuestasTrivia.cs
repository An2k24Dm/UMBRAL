using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarRespuestasTrivia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RespuestaTrivia",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trivia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pregunta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opcion_seleccionada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    es_correcta = table.Column<bool>(type: "boolean", nullable: false),
                    puntos_ganados = table.Column<int>(type: "integer", nullable: false),
                    tiempo_tardado_ms = table.Column<int>(type: "integer", nullable: false),
                    fecha_respuesta_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestaTrivia", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id",
                schema: "sesiones",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "etapa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_participante_identidad_id",
                schema: "sesiones",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "etapa_id", "participante_identidad_id" });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_participante~",
                schema: "sesiones",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "etapa_id", "pregunta_id", "participante_identidad_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespuestaTrivia",
                schema: "sesiones");
        }
    }
}
