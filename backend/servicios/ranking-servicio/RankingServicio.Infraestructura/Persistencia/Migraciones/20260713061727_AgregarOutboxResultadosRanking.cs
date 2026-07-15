using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankingServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarOutboxResultadosRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_ranking",
                schema: "ranking",
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
                    table.PrimaryKey("PK_outbox_ranking", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_ranking_estado_proximo_intento_utc_creado_en_utc",
                schema: "ranking",
                table: "outbox_ranking",
                columns: new[] { "estado", "proximo_intento_utc", "creado_en_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_ranking",
                schema: "ranking");
        }
    }
}
