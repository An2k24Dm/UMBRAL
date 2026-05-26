using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones;

public partial class TablaEventoSalida : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.CreateTable(
            name: "EventoSalida",
            schema: "juegos",
            columns: tabla => new
            {
                id             = tabla.Column<Guid>(type: "uuid", nullable: false),
                tipo           = tabla.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                datos          = tabla.Column<string>(type: "text", nullable: false),
                fecha_creacion = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                procesado      = tabla.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
            },
            constraints: t => t.PrimaryKey("PK_EventoSalida", x => x.id));

        mb.CreateIndex("IX_EventoSalida_procesado", "EventoSalida", "procesado", schema: "juegos");
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropTable(name: "EventoSalida", schema: "juegos");
    }
}
