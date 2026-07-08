using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarJuegoTesoro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvidenciaTesoro",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    busqueda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_enviado = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    es_valida = table.Column<bool>(type: "boolean", nullable: false),
                    puntos_ganados = table.Column<int>(type: "integer", nullable: false),
                    fecha_envio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenciaTesoro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PistaLiberada",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pista_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contenido = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    fecha_liberacion_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PistaLiberada", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                columns: new[] { "sesion_id", "etapa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciaTesoro_sesion_id_etapa_id_participante_identidad_id",
                schema: "sesiones",
                table: "EvidenciaTesoro",
                columns: new[] { "sesion_id", "etapa_id", "participante_identidad_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PistaLiberada_sesion_id_etapa_id",
                schema: "sesiones",
                table: "PistaLiberada",
                columns: new[] { "sesion_id", "etapa_id" });

            migrationBuilder.CreateIndex(
                name: "IX_PistaLiberada_sesion_id_etapa_id_pista_id",
                schema: "sesiones",
                table: "PistaLiberada",
                columns: new[] { "sesion_id", "etapa_id", "pista_id" },
                unique: true,
                filter: "pista_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenciaTesoro",
                schema: "sesiones");

            migrationBuilder.DropTable(
                name: "PistaLiberada",
                schema: "sesiones");
        }
    }
}
