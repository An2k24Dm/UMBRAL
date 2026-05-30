using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AjustarSesionYPatronState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sesion_contenido_id",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "nombre_contenido",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.DropColumn(
                name: "creada_por_nombre_usuario",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.RenameColumn(
                name: "contenido_id",
                schema: "sesiones",
                table: "Sesion",
                newName: "contenido_juego_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_contenido_juego_id",
                schema: "sesiones",
                table: "Sesion",
                column: "contenido_juego_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sesion_contenido_juego_id",
                schema: "sesiones",
                table: "Sesion");

            migrationBuilder.RenameColumn(
                name: "contenido_juego_id",
                schema: "sesiones",
                table: "Sesion",
                newName: "contenido_id");

            migrationBuilder.AddColumn<string>(
                name: "nombre_contenido",
                schema: "sesiones",
                table: "Sesion",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "creada_por_nombre_usuario",
                schema: "sesiones",
                table: "Sesion",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sesion_contenido_id",
                schema: "sesiones",
                table: "Sesion",
                column: "contenido_id");
        }
    }
}
