using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AdaptarBusquedaTesoroSinEtapas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mision_Etapa_etapa_id",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.DropForeignKey(
                name: "FK_Pista_Etapa_etapa_id",
                schema: "juegos",
                table: "Pista");

            migrationBuilder.DropTable(
                name: "Etapa",
                schema: "juegos");

            migrationBuilder.DropIndex(
                name: "IX_Mision_etapa_id",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.RenameColumn(
                name: "etapa_id",
                schema: "juegos",
                table: "Pista",
                newName: "mision_id");

            migrationBuilder.RenameIndex(
                name: "IX_Pista_etapa_id",
                schema: "juegos",
                table: "Pista",
                newName: "IX_Pista_mision_id");

            migrationBuilder.RenameColumn(
                name: "etapa_id",
                schema: "juegos",
                table: "Mision",
                newName: "busqueda_id");

            migrationBuilder.AlterColumn<string>(
                name: "pista_clave",
                schema: "juegos",
                table: "Mision",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // Borrar datos huérfanos: los mision_id/etapa_id de Pista y Mision
            // ya no corresponden a entidades válidas tras el cambio de esquema.
            migrationBuilder.Sql("DELETE FROM juegos.\"Pista\"");
            migrationBuilder.Sql("DELETE FROM juegos.\"Mision\"");

            migrationBuilder.CreateIndex(
                name: "IX_Mision_busqueda_id",
                schema: "juegos",
                table: "Mision",
                column: "busqueda_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Mision_BusquedaTesoro_busqueda_id",
                schema: "juegos",
                table: "Mision",
                column: "busqueda_id",
                principalSchema: "juegos",
                principalTable: "BusquedaTesoro",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pista_Mision_mision_id",
                schema: "juegos",
                table: "Pista",
                column: "mision_id",
                principalSchema: "juegos",
                principalTable: "Mision",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mision_BusquedaTesoro_busqueda_id",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.DropForeignKey(
                name: "FK_Pista_Mision_mision_id",
                schema: "juegos",
                table: "Pista");

            migrationBuilder.DropIndex(
                name: "IX_Mision_busqueda_id",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.RenameColumn(
                name: "mision_id",
                schema: "juegos",
                table: "Pista",
                newName: "etapa_id");

            migrationBuilder.RenameIndex(
                name: "IX_Pista_mision_id",
                schema: "juegos",
                table: "Pista",
                newName: "IX_Pista_etapa_id");

            migrationBuilder.RenameColumn(
                name: "busqueda_id",
                schema: "juegos",
                table: "Mision",
                newName: "etapa_id");

            migrationBuilder.AlterColumn<string>(
                name: "pista_clave",
                schema: "juegos",
                table: "Mision",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "tipo",
                schema: "juegos",
                table: "Mision",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Etapa",
                schema: "juegos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    busqueda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapa", x => x.id);
                    table.ForeignKey(
                        name: "FK_Etapa_BusquedaTesoro_busqueda_id",
                        column: x => x.busqueda_id,
                        principalSchema: "juegos",
                        principalTable: "BusquedaTesoro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mision_etapa_id",
                schema: "juegos",
                table: "Mision",
                column: "etapa_id");

            migrationBuilder.CreateIndex(
                name: "IX_Etapa_busqueda_id",
                schema: "juegos",
                table: "Etapa",
                column: "busqueda_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mision_Etapa_etapa_id",
                schema: "juegos",
                table: "Mision",
                column: "etapa_id",
                principalSchema: "juegos",
                principalTable: "Etapa",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pista_Etapa_etapa_id",
                schema: "juegos",
                table: "Pista",
                column: "etapa_id",
                principalSchema: "juegos",
                principalTable: "Etapa",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
