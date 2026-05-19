using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

public class IniciarSesionManejadorPruebas
{
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IRepositorioIdentidad> _repositorio = new();

    private IniciarSesionManejador CrearManejador() => new(
        _proveedor.Object, _repositorio.Object,
        NullLogger<IniciarSesionManejador>.Instance);

    private static DateTime Ahora => new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime Nac => new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Administrador NuevoAdmin() => Administrador.Crear(
        NombreUsuario.Crear("admin_umbral"), Correo.Crear("admin@umbral.com"),
        NombrePersona.Crear("Ada", "Admin"), DatosContacto.Crear("Av. Bolívar, Caracas", "04143710260"),
        SexoPersona.Femenino, Nac, "ADM-001", Ahora);

    private static Operador NuevoOperador() => Operador.Crear(
        NombreUsuario.Crear("operador01"), Correo.Crear("op@umbral.com"),
        NombrePersona.Crear("Olivia", "Op"), DatosContacto.Crear("Av. Bolívar, Caracas", "04143710260"),
        SexoPersona.Femenino, Nac, "OP-001", Ahora);

    private static Participante NuevoParticipante() => Participante.Crear(
        NombreUsuario.Crear("participante01"), Correo.Crear("par@umbral.com"),
        NombrePersona.Crear("Pablo", "Par"), DatosContacto.Crear("Av. Bolívar, Caracas", "04143710260"),
        SexoPersona.Masculino, Nac, "pablito", Ahora);

    private void TokenValido(string idKc) =>
        _proveedor.Setup(p => p.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoAutenticacionExterna("acc", "ref", 300, "Bearer", idKc));

    [Fact]
    public async Task LoginWeb_Administrador_DevuelveToken()
    {
        TokenValido("kc-admin");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoAdmin());

        var resultado = await CrearManejador().Handle(
            new IniciarSesionComando("admin_umbral", "x", OrigenInicioSesion.Web),
            CancellationToken.None);

        resultado.RutaRedireccion.Should().Be("/administrador");
        resultado.Usuario.Rol.Should().Be("Administrador");
    }

    [Fact]
    public async Task LoginWeb_Operador_DevuelveToken()
    {
        TokenValido("kc-op");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-op", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoOperador());

        var resultado = await CrearManejador().Handle(
            new IniciarSesionComando("operador01", "x", OrigenInicioSesion.Web),
            CancellationToken.None);

        resultado.RutaRedireccion.Should().Be("/operador/sesiones");
    }

    [Fact]
    public async Task LoginWeb_Participante_LanzaAccesoNoPermitido()
    {
        TokenValido("kc-par");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-par", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoParticipante());

        Func<Task> accion = async () => await CrearManejador().Handle(
            new IniciarSesionComando("participante01", "x", OrigenInicioSesion.Web),
            CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoNoPermitidoExcepcion>()
            .WithMessage("*solo pueden iniciar sesión desde la aplicación móvil*");
    }

    [Fact]
    public async Task LoginMovil_Participante_DevuelveToken()
    {
        TokenValido("kc-par");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-par", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoParticipante());

        var resultado = await CrearManejador().Handle(
            new IniciarSesionComando("participante01", "x", OrigenInicioSesion.Movil),
            CancellationToken.None);

        resultado.RutaRedireccion.Should().Be("/participante/sesiones");
    }

    [Fact]
    public async Task LoginMovil_Administrador_LanzaAccesoNoPermitido()
    {
        TokenValido("kc-admin");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoAdmin());

        Func<Task> accion = async () => await CrearManejador().Handle(
            new IniciarSesionComando("admin_umbral", "x", OrigenInicioSesion.Movil),
            CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoNoPermitidoExcepcion>()
            .WithMessage("*Solo los participantes*");
    }

    [Fact]
    public async Task LoginMovil_Operador_LanzaAccesoNoPermitido()
    {
        TokenValido("kc-op");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-op", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoOperador());

        Func<Task> accion = async () => await CrearManejador().Handle(
            new IniciarSesionComando("operador01", "x", OrigenInicioSesion.Movil),
            CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task KeycloakRechaza_LanzaCredencialesInvalidas()
    {
        _proveedor.Setup(p => p.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResultadoAutenticacionExterna?)null);

        Func<Task> accion = async () => await CrearManejador()
            .Handle(new IniciarSesionComando("admin_umbral", "x", OrigenInicioSesion.Web),
                    CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public async Task UsuarioInactivo_LanzaCuentaDesactivada()
    {
        var admin = NuevoAdmin();
        admin.Desactivar();
        TokenValido("kc-admin");
        _repositorio.Setup(r => r.ObtenerPorIdKeycloakAsync("kc-admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        Func<Task> accion = async () => await CrearManejador().Handle(
            new IniciarSesionComando("admin_umbral", "x", OrigenInicioSesion.Web),
            CancellationToken.None);

        await accion.Should().ThrowAsync<CuentaDesactivadaExcepcion>();
    }
}
