using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentidadServicio.Infraestructura.Persistencia.Migraciones;

public partial class InicialIdentidad : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.EnsureSchema(name: "identidad");

        mb.CreateTable(
            name: "Usuario",
            schema: "identidad",
            columns: tabla => new
            {
                id = tabla.Column<Guid>(type: "uuid", nullable: false),
                nombre_usuario = tabla.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                id_keycloak = tabla.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                rol = tabla.Column<int>(type: "integer", nullable: false),
                estado = tabla.Column<int>(type: "integer", nullable: false),
                fecha_registro = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t => t.PrimaryKey("PK_Usuario", x => x.id));

        mb.CreateIndex("IX_Usuario_nombre_usuario", "Usuario", "nombre_usuario", schema: "identidad", unique: true);
        mb.CreateIndex("IX_Usuario_id_keycloak", "Usuario", "id_keycloak", schema: "identidad", unique: true);

        mb.CreateTable(
            name: "Persona",
            schema: "identidad",
            columns: tabla => new
            {
                id = tabla.Column<Guid>(type: "uuid", nullable: false),
                usuario_id = tabla.Column<Guid>(type: "uuid", nullable: false),
                nombre = tabla.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                apellido = tabla.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                correo = tabla.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                direccion = tabla.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                telefono = tabla.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                sexo = tabla.Column<int>(type: "integer", nullable: false),
                fecha_nacimiento = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                fecha_registro = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Persona", x => x.id);
                t.ForeignKey("FK_Persona_Usuario_usuario_id",
                    column: x => x.usuario_id,
                    principalSchema: "identidad", principalTable: "Usuario", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Persona_usuario_id", "Persona", "usuario_id", schema: "identidad", unique: true);
        mb.CreateIndex("IX_Persona_correo", "Persona", "correo", schema: "identidad", unique: true);

        mb.CreateTable(
            name: "Administrador",
            schema: "identidad",
            columns: tabla => new
            {
                id = tabla.Column<Guid>(type: "uuid", nullable: false),
                persona_id = tabla.Column<Guid>(type: "uuid", nullable: false),
                codigo_administrador = tabla.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                fecha_registro = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Administrador", x => x.id);
                t.ForeignKey("FK_Administrador_Persona_persona_id",
                    column: x => x.persona_id,
                    principalSchema: "identidad", principalTable: "Persona", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Administrador_persona_id", "Administrador", "persona_id", schema: "identidad", unique: true);

        mb.CreateTable(
            name: "Operador",
            schema: "identidad",
            columns: tabla => new
            {
                id = tabla.Column<Guid>(type: "uuid", nullable: false),
                persona_id = tabla.Column<Guid>(type: "uuid", nullable: false),
                codigo_operador = tabla.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                fecha_registro = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Operador", x => x.id);
                t.ForeignKey("FK_Operador_Persona_persona_id",
                    column: x => x.persona_id,
                    principalSchema: "identidad", principalTable: "Persona", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Operador_persona_id", "Operador", "persona_id", schema: "identidad", unique: true);
        mb.CreateIndex("IX_Operador_codigo_operador", "Operador", "codigo_operador", schema: "identidad", unique: true);

        mb.CreateTable(
            name: "Participante",
            schema: "identidad",
            columns: tabla => new
            {
                id = tabla.Column<Guid>(type: "uuid", nullable: false),
                persona_id = tabla.Column<Guid>(type: "uuid", nullable: false),
                alias = tabla.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                fecha_registro = tabla.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Participante", x => x.id);
                t.ForeignKey("FK_Participante_Persona_persona_id",
                    column: x => x.persona_id,
                    principalSchema: "identidad", principalTable: "Persona", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        mb.CreateIndex("IX_Participante_persona_id", "Participante", "persona_id", schema: "identidad", unique: true);
        mb.CreateIndex("IX_Participante_alias", "Participante", "alias", schema: "identidad", unique: true);
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropTable("Participante", "identidad");
        mb.DropTable("Operador", "identidad");
        mb.DropTable("Administrador", "identidad");
        mb.DropTable("Persona", "identidad");
        mb.DropTable("Usuario", "identidad");
    }
}
