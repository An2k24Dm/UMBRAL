using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    public partial class AgregarTiemposPuntajes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tiempo_estimado",
                schema: "juegos",
                table: "Pregunta",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "tiempo",
                schema: "juegos",
                table: "BusquedaTesoro",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "puntaje",
                schema: "juegos",
                table: "BusquedaTesoro",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tiempo_estimado", schema: "juegos", table: "Pregunta");
            migrationBuilder.DropColumn(name: "tiempo", schema: "juegos", table: "BusquedaTesoro");
            migrationBuilder.DropColumn(name: "puntaje", schema: "juegos", table: "BusquedaTesoro");
        }
    }
}
