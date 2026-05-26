using FluentAssertions;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.PruebasUnitarias.Servicios.Usuarios;

// HU09 — pruebas del servicio puro AplicadorCambiosUsuario. Cubre la
// detección y aplicación de cambios parciales sobre el agregado Operador.
public class AplicadorCambiosUsuarioPruebas
{
    private static readonly DateTime FechaRegistroOriginal = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaNacimientoOriginal = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Operador OperadorOriginal() => new(
        Guid.NewGuid(),
        NombreUsuario.Crear("operador01"),
        Correo.Crear("operador@umbral.com"),
        EstadoUsuario.Activo,
        FechaRegistroOriginal,
        NombrePersona.Crear("Olivia", "Operadora"),
        DatosContacto.Crear("Av. Libertador, Caracas", "04141111111"),
        SexoPersona.Femenino,
        FechaNacimientoOriginal,
        "OP-001");

    private static AplicadorCambiosUsuario Crear() => new();

    // ============================================================
    // Reglas de "edición parcial"
    // ============================================================

    [Fact]
    public void NombreNulo_NoTocaNombre()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto());
        resultado.CamposActualizados.Should().NotContain("nombre");
        op.NombrePersona.Nombre.Should().Be("Olivia");
    }

    [Fact]
    public void NombreIgualAlActual_NoRegistraCambio()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto { Nombre = "Olivia" });
        resultado.CamposActualizados.Should().NotContain("nombre");
        resultado.HuboCambiosDatosUsuario.Should().BeFalse();
    }

    [Fact]
    public void NombreDiferente_ActualizaDominioYAgregaCampo()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto { Nombre = "Olivia María" });

        resultado.CamposActualizados.Should().Contain("nombre");
        resultado.HuboCambiosDatosUsuario.Should().BeTrue();
        op.NombrePersona.Nombre.Should().Be("Olivia María");
        resultado.DatosKeycloak.Nombre.Should().Be("Olivia María");
    }

    [Fact]
    public void CorreoCambia_AgregaDatosParaKeycloak()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto { Correo = "nuevo@umbral.com" });

        resultado.CamposActualizados.Should().Contain("correo");
        resultado.DatosKeycloak.Correo.Should().Be("nuevo@umbral.com");
        op.Correo.Valor.Should().Be("nuevo@umbral.com");
    }

    [Fact]
    public void NombreUsuarioCambia_AgregaDatosParaKeycloak()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto { NombreUsuario = "olivia.nueva" });

        resultado.CamposActualizados.Should().Contain("nombreUsuario");
        resultado.DatosKeycloak.NombreUsuario.Should().Be("olivia.nueva");
        op.NombreUsuario.Valor.Should().Be("olivia.nueva");
    }

    [Fact]
    public void DatosContactoCambia_SoloSubcamposModificados()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto
        {
            DatosContacto = new DatosContactoDto { Telefono = "04149999999" }
        });

        resultado.CamposActualizados.Should().Contain("datosContacto.telefono");
        resultado.CamposActualizados.Should().NotContain("datosContacto.direccion");
        op.DatosContacto.Telefono.Should().Be("04149999999");
        op.DatosContacto.Direccion.Should().Be("Av. Libertador, Caracas");
    }

    [Fact]
    public void SoloContrasena_NoTocaDominio_MarcaCambiaContrasena()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto
        {
            NuevaContrasena = "Abc1*",
            ConfirmacionContrasena = "Abc1*"
        });

        resultado.HuboCambiosDatosUsuario.Should().BeFalse();
        resultado.CambiaContrasena.Should().BeTrue();
        resultado.NuevaContrasena.Should().Be("Abc1*");
        resultado.RequiereGuardarBaseDatos.Should().BeFalse();
        resultado.RequiereSincronizarKeycloak.Should().BeTrue();
        // El dominio queda intacto.
        op.NombrePersona.Nombre.Should().Be("Olivia");
        op.Correo.Valor.Should().Be("operador@umbral.com");
    }

    [Fact]
    public void Contrasena_NoApareceEnCamposActualizadosConSuValor()
    {
        var op = OperadorOriginal();
        var resultado = Crear().Aplicar(op, new ModificarOperadorSolicitudDto
        {
            NuevaContrasena = "Sup3r*",
            ConfirmacionContrasena = "Sup3r*"
        });

        resultado.CamposActualizados.Should().NotContain(c => c.Contains("Sup3r*"));
    }

    [Fact]
    public void NoModificaEstadoRolFechaRegistro()
    {
        var op = OperadorOriginal();
        var dto = new ModificarOperadorSolicitudDto
        {
            Nombre = "Otra",
            Correo = "otro@umbral.com",
            DatosContacto = new DatosContactoDto { Telefono = "04149999999", Direccion = "Otra dirección 123" }
        };
        Crear().Aplicar(op, dto);

        op.Estado.Should().Be(EstadoUsuario.Activo);
        op.Rol.Should().Be(RolUsuario.Operador);
        op.FechaRegistro.Should().Be(FechaRegistroOriginal);
    }

    // ============================================================
    // Alias (HU10 — solo Participante).
    // ============================================================

    private static IdentidadServicio.Dominio.Entidades.Participante ParticipanteOriginal() =>
        IdentidadServicio.Dominio.Entidades.Participante.Crear(
            NombreUsuario.Crear("participante01"),
            Correo.Crear("pablo@umbral.com"),
            NombrePersona.Crear("Pablo", "Participante"),
            DatosContacto.Crear("Av. Libertador, Caracas", "04141111111"),
            SexoPersona.Masculino,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "sombrita",
            FechaRegistroOriginal);

    [Fact]
    public void Alias_Null_NoTocaParticipante()
    {
        var participante = ParticipanteOriginal();
        var resultado = Crear().Aplicar(
            participante, new IdentidadServicio.Commons.Dtos.ModificarParticipanteSolicitudDto());
        resultado.CamposActualizados.Should().NotContain("alias");
        participante.Alias.Should().Be("sombrita");
    }

    [Fact]
    public void Alias_IgualAlActual_NoRegistraCambio()
    {
        var participante = ParticipanteOriginal();
        var resultado = Crear().Aplicar(participante,
            new IdentidadServicio.Commons.Dtos.ModificarParticipanteSolicitudDto { Alias = "sombrita" });
        resultado.CamposActualizados.Should().NotContain("alias");
        resultado.HuboCambiosDatosUsuario.Should().BeFalse();
    }

    [Fact]
    public void Alias_Diferente_ActualizaYAgrega()
    {
        var participante = ParticipanteOriginal();
        var resultado = Crear().Aplicar(participante,
            new IdentidadServicio.Commons.Dtos.ModificarParticipanteSolicitudDto { Alias = "sombra_99" });

        resultado.CamposActualizados.Should().Contain("alias");
        participante.Alias.Should().Be("sombra_99");
        // El alias NO va a Keycloak.
        resultado.DatosKeycloak.NombreUsuario.Should().BeNull();
        resultado.DatosKeycloak.Correo.Should().BeNull();
        resultado.DatosKeycloak.Nombre.Should().BeNull();
        resultado.DatosKeycloak.Apellido.Should().BeNull();
    }

    [Fact]
    public void VariosCampos_AplicaSoloLosCambiados()
    {
        var op = OperadorOriginal();
        var dto = new ModificarOperadorSolicitudDto
        {
            Nombre = "Otra",
            Apellido = "Operadora",                  // igual al actual
            Sexo = "Otro",
            DatosContacto = new DatosContactoDto
            {
                Direccion = "Av. Sucre, Caracas",
                Telefono = "04241234567"
            }
        };

        var resultado = Crear().Aplicar(op, dto);

        resultado.CamposActualizados.Should().Contain("nombre");
        resultado.CamposActualizados.Should().Contain("sexo");
        resultado.CamposActualizados.Should().Contain("datosContacto.direccion");
        resultado.CamposActualizados.Should().Contain("datosContacto.telefono");
        resultado.CamposActualizados.Should().NotContain("apellido");
    }
}
