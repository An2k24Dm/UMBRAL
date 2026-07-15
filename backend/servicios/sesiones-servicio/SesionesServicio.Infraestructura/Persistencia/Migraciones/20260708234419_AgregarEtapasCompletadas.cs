using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesionesServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarEtapasCompletadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtapaCompletada",
                schema: "sesiones",
                columns: table => new
                {
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_completada_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapaCompletada", x => new { x.sesion_id, x.etapa_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtapaCompletada_sesion_id",
                schema: "sesiones",
                table: "EtapaCompletada",
                column: "sesion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtapaCompletada",
                schema: "sesiones");
        }
    }
}
