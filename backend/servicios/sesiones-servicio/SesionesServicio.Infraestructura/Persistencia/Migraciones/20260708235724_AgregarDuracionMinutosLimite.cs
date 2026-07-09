using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarDuracionMinutosLimite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duracion_minutos_limite",
                schema: "sesiones",
                table: "Sesion",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "duracion_minutos_limite",
                schema: "sesiones",
                table: "Sesion");
        }
    }
}
