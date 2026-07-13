using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarEventoPuntuacionIdRespuestasEvidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "evento_puntuacion_id",
                schema: "sesiones",
                table: "RespuestaTrivia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "evento_puntuacion_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTrivia_evento_puntuacion_id",
                schema: "sesiones",
                table: "RespuestaTrivia",
                column: "evento_puntuacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciaTesoro_evento_puntuacion_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                column: "evento_puntuacion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RespuestaTrivia_evento_puntuacion_id",
                schema: "sesiones",
                table: "RespuestaTrivia");

            migrationBuilder.DropIndex(
                name: "IX_EvidenciaTesoro_evento_puntuacion_id",
                schema: "sesiones",
                table: "EvidenciaTesoro");

            migrationBuilder.DropColumn(
                name: "evento_puntuacion_id",
                schema: "sesiones",
                table: "RespuestaTrivia");

            migrationBuilder.DropColumn(
                name: "evento_puntuacion_id",
                schema: "sesiones",
                table: "EvidenciaTesoro");
        }
    }
}
