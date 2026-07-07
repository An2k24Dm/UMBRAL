using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartidasServicio.Infraestructura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class InicioPersistenciaPartidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "partidas");

            migrationBuilder.CreateTable(
                name: "Partida",
                schema: "partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_fin_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RespuestaTrivia",
                schema: "partidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pregunta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opcion_seleccionada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    es_correcta = table.Column<bool>(type: "boolean", nullable: false),
                    puntos_ganados = table.Column<int>(type: "integer", nullable: false),
                    tiempo_tardado_ms = table.Column<long>(type: "bigint", nullable: false),
                    fecha_respuesta_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestaTrivia", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partida_sesion_id",
                schema: "partidas",
                table: "Partida",
                column: "sesion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_equipo_id",
                schema: "partidas",
                table: "RespuestaTrivia",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_participante_id",
                schema: "partidas",
                table: "RespuestaTrivia",
                column: "participante_id");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id",
                schema: "partidas",
                table: "RespuestaTrivia",
                column: "sesion_id");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_pregunta_id_equipo_id",
                schema: "partidas",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "pregunta_id", "equipo_id" },
                unique: true,
                filter: "equipo_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_pregunta_id_participante_id",
                schema: "partidas",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "pregunta_id", "participante_id" },
                unique: true,
                filter: "equipo_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Partida",
                schema: "partidas");

            migrationBuilder.DropTable(
                name: "RespuestaTrivia",
                schema: "partidas");
        }
    }
}
