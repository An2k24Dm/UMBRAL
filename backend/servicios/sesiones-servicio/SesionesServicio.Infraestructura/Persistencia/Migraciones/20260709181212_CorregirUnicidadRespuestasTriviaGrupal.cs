using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CorregirUnicidadRespuestasTriviaGrupal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_participante~",
                schema: "sesiones",
                table: "RespuestaTrivia");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_equipo_id",
                schema: "sesiones",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "etapa_id", "pregunta_id", "equipo_id" },
                unique: true,
                filter: "equipo_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_participante~",
                schema: "sesiones",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "etapa_id", "pregunta_id", "participante_identidad_id" },
                unique: true,
                filter: "equipo_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_equipo_id",
                schema: "sesiones",
                table: "RespuestaTrivia");

            migrationBuilder.DropIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_participante~",
                schema: "sesiones",
                table: "RespuestaTrivia");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_sesion_id_etapa_id_pregunta_id_participante~",
                schema: "sesiones",
                table: "RespuestaTrivia",
                columns: new[] { "sesion_id", "etapa_id", "pregunta_id", "participante_identidad_id" },
                unique: true);
        }
    }
}
