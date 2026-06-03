using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class InicialJuegos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "juegos");

            migrationBuilder.CreateTable(
                name: "BusquedaTesoro",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    creador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tiempo = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    puntaje = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusquedaTesoro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EventoSalida",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    datos = table.Column<string>(type: "text", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    procesado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoSalida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Mision",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    creador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    dificultad = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mision", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Trivia",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    creador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tiempo_limite_por_pregunta = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trivia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Pista",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    busqueda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contenido = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pista", x => x.id);
                    table.ForeignKey(
                        name: "FK_Pista_BusquedaTesoro_busqueda_id",
                        column: x => x.busqueda_id,
                        principalSchema: "juegos",
                        principalTable: "BusquedaTesoro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Etapa",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    tipo_modo_de_juego = table.Column<int>(type: "integer", nullable: false),
                    modo_de_juego_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapa", x => x.id);
                    table.ForeignKey(
                        name: "FK_Etapa_Mision_mision_id",
                        column: x => x.mision_id,
                        principalSchema: "juegos",
                        principalTable: "Mision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pregunta",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trivia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enunciado = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    puntaje_asignado = table.Column<int>(type: "integer", nullable: false),
                    tiempo_estimado = table.Column<int>(type: "integer", nullable: false, defaultValue: 10)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pregunta", x => x.id);
                    table.ForeignKey(
                        name: "FK_Pregunta_Trivia_trivia_id",
                        column: x => x.trivia_id,
                        principalSchema: "juegos",
                        principalTable: "Trivia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Opcion",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pregunta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    es_correcta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opcion", x => x.id);
                    table.ForeignKey(
                        name: "FK_Opcion_Pregunta_pregunta_id",
                        column: x => x.pregunta_id,
                        principalSchema: "juegos",
                        principalTable: "Pregunta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusquedaTesoro_nombre",
                schema: "juegos",
                table: "BusquedaTesoro",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etapa_mision_id_orden",
                schema: "juegos",
                table: "Etapa",
                columns: new[] { "mision_id", "orden" });

            migrationBuilder.CreateIndex(
                name: "IX_Mision_nombre",
                schema: "juegos",
                table: "Mision",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_pregunta_id",
                schema: "juegos",
                table: "Opcion",
                column: "pregunta_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pista_busqueda_id",
                schema: "juegos",
                table: "Pista",
                column: "busqueda_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pregunta_trivia_id",
                schema: "juegos",
                table: "Pregunta",
                column: "trivia_id");

            migrationBuilder.CreateIndex(
                name: "IX_Trivia_nombre",
                schema: "juegos",
                table: "Trivia",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Etapa",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "EventoSalida",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "Opcion",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "Pista",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "Mision",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "Pregunta",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "BusquedaTesoro",
                schema: "juegos");

            migrationBuilder.DropTable(
                name: "Trivia",
                schema: "juegos");
        }
    }
}
