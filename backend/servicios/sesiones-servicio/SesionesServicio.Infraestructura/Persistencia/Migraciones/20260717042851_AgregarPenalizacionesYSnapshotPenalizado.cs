using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPenalizacionesYSnapshotPenalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "puntos_penalizados",
                schema: "sesiones",
                table: "Participante",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "puntos_penalizados",
                schema: "sesiones",
                table: "Equipo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PenalizacionSesion",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_objetivo = table.Column<int>(type: "integer", nullable: false),
                    participante_sesion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    puntos = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    operador_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aplicada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    procesada_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    puntaje_resultante = table.Column<long>(type: "bigint", nullable: true),
                    estado_procesamiento = table.Column<int>(type: "integer", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PenalizacionSesion",
                schema: "sesiones");

            migrationBuilder.DropColumn(
                name: "puntos_penalizados",
                schema: "sesiones",
                table: "Participante");

            migrationBuilder.DropColumn(
                name: "puntos_penalizados",
                schema: "sesiones",
                table: "Equipo");
        }
    }
}
