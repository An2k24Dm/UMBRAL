using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankingServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class RefactorizarDominioRankingPorSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entradas_ranking_equipo",
                schema: "ranking");

            migrationBuilder.DropTable(
                name: "entradas_ranking_participante",
                schema: "ranking");

            migrationBuilder.DropTable(
                name: "ranking_global_participante",
                schema: "ranking");

            migrationBuilder.CreateTable(
                name: "rankings",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rankings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ranking_equipos",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntaje = table.Column<long>(type: "bigint", nullable: false),
                    ranking_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_equipos", x => x.id);
                    table.ForeignKey(
                        name: "FK_ranking_equipos_rankings_ranking_id",
                        column: x => x.ranking_id,
                        principalSchema: "ranking",
                        principalTable: "rankings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ranking_participantes",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    puntaje = table.Column<long>(type: "bigint", nullable: false),
                    ranking_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_participantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_ranking_participantes_rankings_ranking_id",
                        column: x => x.ranking_id,
                        principalSchema: "ranking",
                        principalTable: "rankings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ranking_equipos_ranking_id_equipo_id",
                schema: "ranking",
                table: "ranking_equipos",
                columns: new[] { "ranking_id", "equipo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ranking_participantes_ranking_id_participante_sesion_id",
                schema: "ranking",
                table: "ranking_participantes",
                columns: new[] { "ranking_id", "participante_sesion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rankings_sesion_id",
                schema: "ranking",
                table: "rankings",
                column: "sesion_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ranking_equipos",
                schema: "ranking");

            migrationBuilder.DropTable(
                name: "ranking_participantes",
                schema: "ranking");

            migrationBuilder.DropTable(
                name: "rankings",
                schema: "ranking");

            migrationBuilder.CreateTable(
                name: "entradas_ranking_equipo",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapas_completadas = table.Column<int>(type: "integer", nullable: false),
                    nombre_equipo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    posicion = table.Column<int>(type: "integer", nullable: false),
                    puntaje_total = table.Column<int>(type: "integer", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ultima_actualizacion_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entradas_ranking_equipo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entradas_ranking_participante",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapas_completadas = table.Column<int>(type: "integer", nullable: false),
                    nombre_participante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posicion = table.Column<int>(type: "integer", nullable: false),
                    puntaje_total = table.Column<int>(type: "integer", nullable: false),
                    respuestas_correctas = table.Column<int>(type: "integer", nullable: false),
                    respuestas_totales = table.Column<int>(type: "integer", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ultima_actualizacion_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entradas_ranking_participante", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ranking_global_participante",
                schema: "ranking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapas_completadas_total = table.Column<int>(type: "integer", nullable: false),
                    nombre_participante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntaje_acumulado = table.Column<long>(type: "bigint", nullable: false),
                    sesiones_jugadas = table.Column<int>(type: "integer", nullable: false),
                    ultima_actualizacion_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_global_participante", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entradas_ranking_equipo_sesion_id_equipo_id",
                schema: "ranking",
                table: "entradas_ranking_equipo",
                columns: new[] { "sesion_id", "equipo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entradas_ranking_equipo_sesion_id_posicion",
                schema: "ranking",
                table: "entradas_ranking_equipo",
                columns: new[] { "sesion_id", "posicion" });

            migrationBuilder.CreateIndex(
                name: "IX_entradas_ranking_participante_sesion_id_participante_identi~",
                schema: "ranking",
                table: "entradas_ranking_participante",
                columns: new[] { "sesion_id", "participante_identidad_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entradas_ranking_participante_sesion_id_posicion",
                schema: "ranking",
                table: "entradas_ranking_participante",
                columns: new[] { "sesion_id", "posicion" });

            migrationBuilder.CreateIndex(
                name: "IX_ranking_global_participante_participante_identidad_id",
                schema: "ranking",
                table: "ranking_global_participante",
                column: "participante_identidad_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ranking_global_participante_puntaje_acumulado",
                schema: "ranking",
                table: "ranking_global_participante",
                column: "puntaje_acumulado");
        }
    }
}
