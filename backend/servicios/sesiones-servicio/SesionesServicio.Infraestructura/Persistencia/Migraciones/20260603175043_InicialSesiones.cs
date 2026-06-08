using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class InicialSesiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sesiones");

            migrationBuilder.CreateTable(
                name: "Sesion",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_sesion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fecha_programada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    codigo_acceso = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    operador_creador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_finalizacion_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Equipo",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    lider_participante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntaje = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipo", x => x.id);
                    table.ForeignKey(
                        name: "FK_Equipo_Sesion_sesion_id",
                        column: x => x.sesion_id,
                        principalSchema: "sesiones",
                        principalTable: "Sesion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participante",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participante_identidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    puntaje = table.Column<int>(type: "integer", nullable: false),
                    fecha_union_sesion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_union_equipo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participante", x => x.id);
                    table.ForeignKey(
                        name: "FK_Participante_Sesion_sesion_id",
                        column: x => x.sesion_id,
                        principalSchema: "sesiones",
                        principalTable: "Sesion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionMision",
                schema: "sesiones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionMision", x => x.id);
                    table.ForeignKey(
                        name: "FK_SesionMision_Sesion_sesion_id",
                        column: x => x.sesion_id,
                        principalSchema: "sesiones",
                        principalTable: "Sesion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipo_sesion_id_nombre",
                schema: "sesiones",
                table: "Equipo",
                columns: new[] { "sesion_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participante_equipo_id",
                schema: "sesiones",
                table: "Participante",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Participante_sesion_id_participante_identidad_id",
                schema: "sesiones",
                table: "Participante",
                columns: new[] { "sesion_id", "participante_identidad_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_codigo_acceso",
                schema: "sesiones",
                table: "Sesion",
                column: "codigo_acceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_estado",
                schema: "sesiones",
                table: "Sesion",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_fecha_programada",
                schema: "sesiones",
                table: "Sesion",
                column: "fecha_programada");

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_operador_creador_id",
                schema: "sesiones",
                table: "Sesion",
                column: "operador_creador_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_tipo_sesion",
                schema: "sesiones",
                table: "Sesion",
                column: "tipo_sesion");

            migrationBuilder.CreateIndex(
                name: "IX_SesionMision_mision_id",
                schema: "sesiones",
                table: "SesionMision",
                column: "mision_id");

            migrationBuilder.CreateIndex(
                name: "IX_SesionMision_sesion_id_mision_id",
                schema: "sesiones",
                table: "SesionMision",
                columns: new[] { "sesion_id", "mision_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipo",
                schema: "sesiones");

            migrationBuilder.DropTable(
                name: "Participante",
                schema: "sesiones");

            migrationBuilder.DropTable(
                name: "SesionMision",
                schema: "sesiones");

            migrationBuilder.DropTable(
                name: "Sesion",
                schema: "sesiones");
        }
    }
}
