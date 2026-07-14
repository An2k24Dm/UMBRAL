using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarTipoPistaGps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tipo",
                schema: "juegos",
                table: "Pista",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "latitud",
                schema: "juegos",
                table: "Pista",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitud",
                schema: "juegos",
                table: "Pista",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "juegos",
                table: "Pista");

            migrationBuilder.DropColumn(
                name: "latitud",
                schema: "juegos",
                table: "Pista");

            migrationBuilder.DropColumn(
                name: "longitud",
                schema: "juegos",
                table: "Pista");
        }
    }
}
