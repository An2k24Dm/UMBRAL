using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarTablaPistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pista",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contenido = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pista", x => x.id);
                    table.ForeignKey(
                        name: "FK_Pista_Etapa_etapa_id",
                        column: x => x.etapa_id,
                        principalSchema: "juegos",
                        principalTable: "Etapa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pista_etapa_id",
                schema: "juegos",
                table: "Pista",
                column: "etapa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pista",
                schema: "juegos");
        }
    }
}
