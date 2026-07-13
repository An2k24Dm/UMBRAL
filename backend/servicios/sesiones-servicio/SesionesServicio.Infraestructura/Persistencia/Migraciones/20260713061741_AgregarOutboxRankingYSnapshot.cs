using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarOutboxRankingYSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_ranking_utc",
                schema: "sesiones",
                table: "Participante",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_ranking_utc",
                schema: "sesiones",
                table: "Equipo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutboxRanking",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    routing_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    creado_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enviado_en_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    proximo_intento_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultimo_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxRanking", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRanking_estado_proximo_intento_utc_creado_en_utc",
                schema: "sesiones",
                table: "OutboxRanking",
                columns: new[] { "estado", "proximo_intento_utc", "creado_en_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxRanking",
                schema: "sesiones");

            migrationBuilder.DropColumn(
                name: "snapshot_ranking_utc",
                schema: "sesiones",
                table: "Participante");

            migrationBuilder.DropColumn(
                name: "snapshot_ranking_utc",
                schema: "sesiones",
                table: "Equipo");
        }
    }
}
