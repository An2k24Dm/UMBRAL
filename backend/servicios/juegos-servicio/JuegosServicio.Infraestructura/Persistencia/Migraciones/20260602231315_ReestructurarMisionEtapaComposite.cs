using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ReestructurarMisionEtapaComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "pista_clave",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.RenameColumn(
                name: "mision_id",
                schema: "juegos",
                table: "Pista",
                newName: "busqueda_id");

            migrationBuilder.RenameIndex(
                name: "IX_Pista_mision_id",
                schema: "juegos",
                table: "Pista",
                newName: "IX_Pista_busqueda_id");

            migrationBuilder.RenameColumn(
                name: "titulo",
                schema: "juegos",
                table: "Mision",
                newName: "nombre");

            migrationBuilder.RenameColumn(
                name: "tipo",
                schema: "juegos",
                table: "Mision",
                newName: "estado");

            migrationBuilder.RenameColumn(
                name: "busqueda_id",
                schema: "juegos",
                table: "Mision",
                newName: "creador_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_creacion",
                schema: "juegos",
                table: "Mision",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            // Limpiar datos cuya estructura es incompatible con el nuevo esquema.
            // Pista.busqueda_id (antes mision_id) contiene IDs de Mision, no de
            // BusquedaTesoro, por lo que violaría el FK. Mision también se reinicia
            // porque sus columnas cambiaron de significado (busqueda_id → creador_id).
            migrationBuilder.Sql("DELETE FROM juegos.\"Pista\"");
            migrationBuilder.Sql("DELETE FROM juegos.\"Mision\"");

            migrationBuilder.CreateIndex(
                name: "IX_Mision_nombre",
                schema: "juegos",
                table: "Mision",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etapa_mision_id_orden",
                schema: "juegos",
                table: "Etapa",
                columns: new[] { "mision_id", "orden" });

            migrationBuilder.AddForeignKey(
                name: "FK_Pista_BusquedaTesoro_busqueda_id",
                schema: "juegos",
                table: "Pista",
                column: "busqueda_id",
                principalSchema: "juegos",
                principalTable: "BusquedaTesoro",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pista_BusquedaTesoro_busqueda_id",
                schema: "juegos",
                table: "Pista");

            migrationBuilder.DropTable(
                name: "Etapa",
                schema: "juegos");

            migrationBuilder.DropIndex(
                name: "IX_Mision_nombre",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.DropColumn(
                name: "fecha_creacion",
                schema: "juegos",
                table: "Mision");

            migrationBuilder.RenameColumn(
                name: "busqueda_id",
                schema: "juegos",
                table: "Pista",
                newName: "mision_id");

            migrationBuilder.RenameIndex(
                name: "IX_Pista_busqueda_id",
                schema: "juegos",
                table: "Pista",
                newName: "IX_Pista_mision_id");

            migrationBuilder.RenameColumn(
                name: "nombre",
                schema: "juegos",
                table: "Mision",
                newName: "titulo");

            migrationBuilder.RenameColumn(
                name: "estado",
                schema: "juegos",
                table: "Mision",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "creador_id",
                schema: "juegos",
                table: "Mision",
                newName: "busqueda_id");

            migrationBuilder.AddColumn<string>(
                name: "pista_clave",
                schema: "juegos",
                table: "Mision",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

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
    }
}
