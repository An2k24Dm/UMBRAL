using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankingServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class QuitarPuntajeEtapaParticipanteRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ranking_puntajes_etapa",
                schema: "ranking");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ranking_puntajes_etapa",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntaje = table.Column<long>(type: "bigint", nullable: false),
                    ranking_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_puntajes_etapa", x => x.id);
                    table.ForeignKey(
                        name: "FK_ranking_puntajes_etapa_rankings_ranking_id",
                        column: x => x.ranking_id,
                        principalSchema: "ranking",
                        principalTable: "rankings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ranking_puntajes_etapa_ranking_id_mision_id_etapa_id_partic~",
                schema: "ranking",
                table: "ranking_puntajes_etapa",
                columns: new[] { "ranking_id", "mision_id", "etapa_id", "participante_sesion_id" },
                unique: true);
        }
    }
}
