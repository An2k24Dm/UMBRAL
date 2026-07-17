using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RankingServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPuntosPenalizadosRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "puntos_penalizados",
                schema: "ranking",
                table: "ranking_participantes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "puntos_penalizados",
                schema: "ranking",
                table: "ranking_equipos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "puntos_penalizados",
                schema: "ranking",
                table: "ranking_participantes");

            migrationBuilder.DropColumn(
                name: "puntos_penalizados",
                schema: "ranking",
                table: "ranking_equipos");
        }
    }
}
