using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentidadServicio.Infraestructura.Persistencia.Migraciones;

// HU02 — Códigos OP-### / AD-### generados por backend.
// El código de operador ya tenía índice único desde la migración inicial; aquí
// solo añadimos el índice único filtrado sobre Administrador.codigo_administrador.
public partial class AgregarCodigosUnicosUsuario : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.CreateIndex(
            name: "IX_Administrador_codigo_administrador",
            schema: "identidad",
            table: "Administrador",
            column: "codigo_administrador",
            unique: true,
            filter: "\"codigo_administrador\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropIndex(
            name: "IX_Administrador_codigo_administrador",
            schema: "identidad",
            table: "Administrador");
    }
}
