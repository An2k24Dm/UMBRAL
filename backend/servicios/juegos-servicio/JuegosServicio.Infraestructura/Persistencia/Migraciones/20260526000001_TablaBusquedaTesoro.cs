using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones;

public partial class BusquedaTesoroEtapaMision : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.CreateTable(
            name: "BusquedaTesoro",
            schema: "juegos",
            columns: tabla => new
            {
                id             = tabla.Column<Guid>(type: "uuid", nullable: false),
                nombre         = tabla.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                descripcion    = tabla.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                creador_id     = tabla.Column<Guid>(type: "uuid", nullable: false),
                estado         = tabla.Column<int>(type: "integer", nullable: false),
                fecha_creacion = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t => t.PrimaryKey("PK_BusquedaTesoro", x => x.id));

        mb.CreateIndex("IX_BusquedaTesoro_nombre", "BusquedaTesoro", "nombre", schema: "juegos", unique: true);

        mb.CreateTable(
            name: "Etapa",
            schema: "juegos",
            columns: tabla => new
            {
                id          = tabla.Column<Guid>(type: "uuid", nullable: false),
                busqueda_id = tabla.Column<Guid>(type: "uuid", nullable: false),
                titulo      = tabla.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                descripcion = tabla.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                orden       = tabla.Column<int>(type: "integer", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Etapa", x => x.id);
                t.ForeignKey(
                    name: "FK_Etapa_BusquedaTesoro_busqueda_id",
                    column: x => x.busqueda_id,
                    principalSchema: "juegos",
                    principalTable: "BusquedaTesoro",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Etapa_busqueda_id", "Etapa", "busqueda_id", schema: "juegos");

        mb.CreateTable(
            name: "Mision",
            schema: "juegos",
            columns: tabla => new
            {
                id          = tabla.Column<Guid>(type: "uuid", nullable: false),
                etapa_id    = tabla.Column<Guid>(type: "uuid", nullable: false),
                titulo      = tabla.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                descripcion = tabla.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                tipo        = tabla.Column<int>(type: "integer", nullable: false),
                pista_clave = tabla.Column<string>(type: "text", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Mision", x => x.id);
                t.ForeignKey(
                    name: "FK_Mision_Etapa_etapa_id",
                    column: x => x.etapa_id,
                    principalSchema: "juegos",
                    principalTable: "Etapa",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Mision_etapa_id", "Mision", "etapa_id", schema: "juegos");
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropTable(name: "Mision", schema: "juegos");
        mb.DropTable(name: "Etapa", schema: "juegos");
        mb.DropTable(name: "BusquedaTesoro", schema: "juegos");
    }
}
