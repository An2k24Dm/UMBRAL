using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;
using Moq;
using Xunit;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU12 — regla de aplicación: un usuario autenticado inactivo no puede usar el
// sistema. La traducción a HTTP la hace el middleware de Presentación.
public class ValidadorAccesoUsuarioActivoPruebas
{
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Nac = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRepositorioUsuariosLectura> _repositorio = new();

    private ValidadorAccesoUsuarioActivo Crear() => new(_repositorio.Object);

    private static Operador NuevoOperador() => Operador.Crear(
        NombreUsuario.Crear("operador01"), Correo.Crear("op@umbral.com"),
        NombrePersona.Crear("Olivia", "Op"),
        DatosContacto.Crear("Av. Bolívar, Caracas", "04143710260"),
        SexoPersona.Femenino, Nac, "OP-001", Ahora);

    [Fact]
    public async Task UsuarioActivo_DevuelvePermitido()
    {
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoOperador());

        var resultado = await Crear().ValidarAsync("kc-1", CancellationToken.None);

        resultado.PuedeAcceder.Should().BeTrue();
        resultado.Codigo.Should().BeNull();
    }

    [Fact]
    public async Task UsuarioInactivo_DevuelveBloqueado()
    {
        var operador = NuevoOperador();
        operador.Desactivar();
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(operador);

        var resultado = await Crear().ValidarAsync("kc-1", CancellationToken.None);

        resultado.PuedeAcceder.Should().BeFalse();
        resultado.Codigo.Should().Be("CUENTA_DESACTIVADA");
        resultado.Mensaje.Should().Be("La cuenta se encuentra desactivada.");
    }

    [Fact]
    public async Task UsuarioNoExiste_DevuelvePermitido_MantieneComportamiento()
    {
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var resultado = await Crear().ValidarAsync("kc-x", CancellationToken.None);

        resultado.PuedeAcceder.Should().BeTrue();
    }
}
