using FluentAssertions;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;
using IdentidadServicio.Infraestructura.Persistencia;
using IdentidadServicio.Infraestructura.Persistencia.Mapeadores;

namespace IdentidadServicio.PruebasUnitarias.Persistencia;

public class IdentidadMapeadorPruebas
{
    private readonly IdentidadMapeador _mapeador = new();
    private static DateTime Ahora => new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime Nac => new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Administrador_AModelos_GuardaCorreoEnPersonaYNoEnUsuario()
    {
        var admin = Administrador.Crear(
            NombreUsuario.Crear("admin_umbral"), Correo.Crear("ada@umbral.com"),
            NombrePersona.Crear("Ada", "Admin"),
            DatosContacto.Crear("Calle 1", "04141234567"),
            SexoPersona.Femenino, Nac, "ADM-001", Ahora);

        var m = _mapeador.AModelos(admin, "kc-admin");

        m.Usuario.NombreUsuario.Should().Be("admin_umbral");
        m.Usuario.IdKeycloak.Should().Be("kc-admin");
        m.Persona.Correo.Should().Be("ada@umbral.com");
        m.Persona.UsuarioId.Should().Be(m.Usuario.Id);
        m.Administrador.PersonaId.Should().Be(m.Persona.Id);
        m.Administrador.CodigoAdministrador.Should().Be("ADM-001");
    }

    [Fact]
    public void UsuarioModelo_NoTieneCorreoNiEmail()
    {
        typeof(UsuarioModelo).GetProperty("Correo").Should().BeNull();
        typeof(UsuarioModelo).GetProperty("Email").Should().BeNull();
    }

    [Fact]
    public void PersonaModelo_TieneCorreo()
    {
        typeof(PersonaModelo).GetProperty("Correo").Should().NotBeNull();
    }

    [Fact]
    public void Operador_AModelos_GuardaCodigoYCorreoEnPersona()
    {
        var op = Operador.Crear(
            NombreUsuario.Crear("operador01"), Correo.Crear("op@umbral.com"),
            NombrePersona.Crear("Olivia", "Op"), DatosContacto.Crear("Av. Bolívar", "04143710260"),
            SexoPersona.Femenino, Nac, "OP-001", Ahora);

        var m = _mapeador.AModelos(op, "kc-op");
        m.Operador.CodigoOperador.Should().Be("OP-001");
        m.Persona.Correo.Should().Be("op@umbral.com");
        m.Usuario.IdKeycloak.Should().Be("kc-op");
    }

    [Fact]
    public void Participante_AModelos_GuardaAliasYCorreoEnPersona()
    {
        var par = Participante.Crear(
            NombreUsuario.Crear("participante01"), Correo.Crear("par@umbral.com"),
            NombrePersona.Crear("Pablo", "Par"), DatosContacto.Crear("Av. Bolívar", "04143710260"),
            SexoPersona.Masculino, Nac, "pablito", Ahora);

        var m = _mapeador.AModelos(par, "kc-par");
        m.Participante.Alias.Should().Be("pablito");
        m.Persona.Correo.Should().Be("par@umbral.com");
    }

    [Fact]
    public void MapeoInverso_RecuperaAdministrador()
    {
        var admin = Administrador.Crear(
            NombreUsuario.Crear("admin_umbral"), Correo.Crear("ada@umbral.com"),
            NombrePersona.Crear("Ada", "Admin"),
            DatosContacto.Crear("Av. Bolívar", "04141234567"),
            SexoPersona.Femenino, Nac, "ADM-001", Ahora);
        var modelos = _mapeador.AModelos(admin, "kc-admin");

        var reconstruido = _mapeador.AAdministrador(
            modelos.Usuario, modelos.Persona, modelos.Administrador);

        reconstruido.NombreUsuario.Valor.Should().Be("admin_umbral");
        reconstruido.Correo.Valor.Should().Be("ada@umbral.com");
        reconstruido.NombrePersona.Nombre.Should().Be("Ada");
        reconstruido.Sexo.Should().Be(SexoPersona.Femenino);
        reconstruido.CodigoAdministrador.Should().Be("ADM-001");
    }
}
