using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class SincronizarModeloJuegos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventoSalida_procesado",
                schema: "juegos",
                table: "EventoSalida");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EventoSalida_procesado",
                schema: "juegos",
                table: "EventoSalida",
                column: "procesado");
        }
    }
}
