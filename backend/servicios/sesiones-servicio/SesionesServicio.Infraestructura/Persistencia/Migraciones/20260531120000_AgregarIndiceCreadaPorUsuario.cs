using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarIndiceCreadaPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HU34 — El listado del Operador filtra por creada_por_usuario_id.
            // Agregamos un índice para que el plan de PostgreSQL aproveche
            // la búsqueda por ese campo en sesiones con muchas filas.
            migrationBuilder.CreateIndex(
                name: "IX_Sesion_creada_por_usuario_id",
                schema: "sesiones",
                table: "Sesion",
                column: "creada_por_usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sesion_creada_por_usuario_id",
                schema: "sesiones",
                table: "Sesion");
        }
    }
}
