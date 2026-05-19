using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentidadServicio.Infraestructura.Persistencia.Migraciones;

public partial class AgregarValidacionesUnicasUsuario : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        // Índice único filtrado: el teléfono es opcional, pero cuando viene
        // informado debe ser único entre personas (HU02).
        mb.CreateIndex(
            name: "IX_Persona_telefono",
            schema: "identidad",
            table: "Persona",
            column: "telefono",
            unique: true,
            filter: "\"telefono\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropIndex(
            name: "IX_Persona_telefono",
            schema: "identidad",
            table: "Persona");
    }
}
