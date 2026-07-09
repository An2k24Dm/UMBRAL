using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegosServicio.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarCodigoQrBusquedaTesoro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codigo_qr",
                schema: "juegos",
                table: "BusquedaTesoro",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "codigo_qr",
                schema: "juegos",
                table: "BusquedaTesoro");
        }
    }
}
