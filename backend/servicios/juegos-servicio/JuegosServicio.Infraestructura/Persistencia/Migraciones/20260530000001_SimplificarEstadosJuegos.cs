using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones;

public partial class SimplificarEstadosJuegos : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        // Archivada (2) → Inactiva (0). Borrador (0) ya es Inactiva; Activa (1) no cambia.
        mb.Sql("UPDATE juegos.\"Trivia\" SET estado = 0 WHERE estado = 2;");
        mb.Sql("UPDATE juegos.\"BusquedaTesoro\" SET estado = 0 WHERE estado = 2;");
    }

    protected override void Down(MigrationBuilder mb)
    {
        // No se puede recuperar cuáles eran Borrador y cuáles Archivada.
    }
}
