using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones;

public partial class InicialJuegos : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.EnsureSchema(name: "juegos");

        mb.CreateTable(
            name: "Trivia",
            schema: "juegos",
            columns: tabla => new
            {
                id                          = tabla.Column<Guid>(type: "uuid", nullable: false),
                nombre                      = tabla.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                descripcion                 = tabla.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                creador_id                  = tabla.Column<Guid>(type: "uuid", nullable: false),
                tiempo_limite_por_pregunta  = tabla.Column<int>(type: "integer", nullable: false),
                estado                      = tabla.Column<int>(type: "integer", nullable: false),
                fecha_creacion              = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t => t.PrimaryKey("PK_Trivia", x => x.id));

        mb.CreateIndex("IX_Trivia_nombre", "Trivia", "nombre", schema: "juegos", unique: true);

        mb.CreateTable(
            name: "Pregunta",
            schema: "juegos",
            columns: tabla => new
            {
                id               = tabla.Column<Guid>(type: "uuid", nullable: false),
                trivia_id        = tabla.Column<Guid>(type: "uuid", nullable: false),
                enunciado        = tabla.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                puntaje_asignado = tabla.Column<int>(type: "integer", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Pregunta", x => x.id);
                t.ForeignKey(
                    name: "FK_Pregunta_Trivia_trivia_id",
                    column: x => x.trivia_id,
                    principalSchema: "juegos",
                    principalTable: "Trivia",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Pregunta_trivia_id", "Pregunta", "trivia_id", schema: "juegos");

        mb.CreateTable(
            name: "Opcion",
            schema: "juegos",
            columns: tabla => new
            {
                id           = tabla.Column<Guid>(type: "uuid", nullable: false),
                pregunta_id  = tabla.Column<Guid>(type: "uuid", nullable: false),
                texto        = tabla.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                es_correcta  = tabla.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Opcion", x => x.id);
                t.ForeignKey(
                    name: "FK_Opcion_Pregunta_pregunta_id",
                    column: x => x.pregunta_id,
                    principalSchema: "juegos",
                    principalTable: "Pregunta",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Opcion_pregunta_id", "Opcion", "pregunta_id", schema: "juegos");
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropTable("Opcion",    "juegos");
        mb.DropTable("Pregunta",  "juegos");
        mb.DropTable("Trivia",    "juegos");
    }
}
