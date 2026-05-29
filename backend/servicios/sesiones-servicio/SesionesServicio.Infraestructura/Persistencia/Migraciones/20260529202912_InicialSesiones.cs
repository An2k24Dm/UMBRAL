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
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_juego = table.Column<int>(type: "integer", nullable: false),
                    contenido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_contenido = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modo = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fecha_programada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    creada_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creada_por_nombre_usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesion", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_contenido_id",
                schema: "sesiones",
                table: "Sesion",
                column: "contenido_id");

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
                name: "IX_Sesion_tipo_juego",
                schema: "sesiones",
                table: "Sesion",
                column: "tipo_juego");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sesion",
                schema: "sesiones");
        }
    }
}
