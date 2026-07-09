using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CorregirEvidenciaTesoroPorEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_participante_identidad_id",
                schema: "sesiones",
                table: "EvidenciaTesoro");

            migrationBuilder.AddColumn<Guid>(
                name: "equipo_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_equipo_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                columns: new[] { "sesion_id", "etapa_id", "equipo_id" },
                unique: true,
                filter: "equipo_id IS NOT NULL AND es_valida");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_participante_identidad_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                columns: new[] { "sesion_id", "etapa_id", "participante_identidad_id" },
                unique: true,
                filter: "equipo_id IS NULL AND es_valida");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_equipo_id",
                schema: "sesiones",
                table: "EvidenciaTesoro");

            migrationBuilder.DropIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_participante_identidad_id",
                schema: "sesiones",
                table: "EvidenciaTesoro");

            migrationBuilder.DropColumn(
                name: "equipo_id",
                schema: "sesiones",
                table: "EvidenciaTesoro");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_participante_identidad_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                columns: new[] { "sesion_id", "etapa_id", "participante_identidad_id" },
                unique: true);
        }
    }
}
