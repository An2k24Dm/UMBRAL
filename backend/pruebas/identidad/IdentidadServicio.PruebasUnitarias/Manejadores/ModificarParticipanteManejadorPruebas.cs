using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU10 — pruebas del coordinador. El manejador delega validación a sus
// validadores específicos y la detección de cambios a AplicadorCambiosUsuario.
// Aquí cubrimos el flujo de coordinación específico de HU10:
//  * identificación del usuario por IdKeycloak del token,
//  * caso "sin cambios",
//  * orden contraseña → datos → guardar,
//  * fallos de Keycloak,
//  * confidencialidad de la contraseña.
public class ModificarParticipanteManejadorPruebas
{
    private readonly Mock<IRepositorioParticipantes> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IValidador<ModificarParticipanteComando>> _validador = new();
    private readonly Mock<IValidadorAsincrono<ModificarParticipanteComando>> _validadorUnicidad = new();

    public ModificarParticipanteManejadorPruebas()
    {
        _validador
            .Setup(v => v.Validar(It.IsAny<ModificarParticipanteComando>()))
            .Returns(ResultadoValidacion.Exitoso());
        _validadorUnicidad
            .Setup(v => v.ValidarAsync(
                It.IsAny<ModificarParticipanteComando>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoValidacion.Exitoso());
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-participante");
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ModificarParticipanteManejador CrearManejador(
        ILogger<ModificarParticipanteManejador>? logger = null)
    {
        var fabrica = new FabricaEstrategiaMapeoPerfilUsuario(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilParticipante()
        });
        return new ModificarParticipanteManejador(
            _repositorio.Object,
            _unidad.Object,
            _proveedor.Object,
            _validador.Object,
            _validadorUnicidad.Object,
            new AplicadorCambiosUsuario(),
            fabrica,
            logger ?? NullLogger<ModificarParticipanteManejador>.Instance);
    }

    private void EncolarParticipante(string idKeycloak, Participante p)
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdKeycloakAsync(idKeycloak, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);
    }

    [Fact]
    public async Task RechazaUsuarioNoParticipante()
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdKeycloakAsync("kc-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Participante?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", new ModificarParticipanteSolicitudDto { Nombre = "X" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public async Task ObtieneParticipanteDesdeIdKeycloakDelToken()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", new ModificarParticipanteSolicitudDto { Nombre = "Pablo Nuevo" }),
            CancellationToken.None);

        _repositorio.Verify(r => r.ObtenerPorIdKeycloakAsync("kc-x", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SinCambios_NoLlamaRepositorioKeycloakNiUnidadTrabajo()
    {
        var p = UsuariosDePrueba.NuevoParticipante(nombreUsuario: "participante01", nombre: "Pablo");
        EncolarParticipante("kc-x", p);

        // DTO con los mismos valores actuales → no hay diff.
        var dto = new ModificarParticipanteSolicitudDto
        {
            NombreUsuario = "participante01",
            Nombre = "Pablo"
        };

        var resultado = await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeFalse();
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SoloContrasena_LlamaCambiarContrasenaAsync_NoGuardaBase()
    {
        // HU10 — el Participante SÍ puede cambiar su contraseña desde la
        // app móvil. La contraseña no es temporal (la define él mismo).
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        var dto = new ModificarParticipanteSolicitudDto
        {
            NuevaContrasena = "Abc1*",
            ConfirmacionContrasena = "Abc1*"
        };
        var resultado = await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeTrue();
        resultado.CamposActualizados.Should().Contain("contrasena");
        _proveedor.Verify(p => p.CambiarContrasenaAsync(
            "kc-x", "Abc1*", It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DatosYContrasena_OrdenCorrecto_YGuardaBase()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        var orden = new List<string>();
        _proveedor.Setup(p => p.CambiarContrasenaAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("contrasena")).Returns(Task.CompletedTask);
        _proveedor.Setup(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(), It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("datos")).Returns(Task.CompletedTask);
        _unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("guardar")).Returns(Task.CompletedTask);

        var dto = new ModificarParticipanteSolicitudDto
        {
            Correo = "nuevo@umbral.com",
            NuevaContrasena = "Abc1*",
            ConfirmacionContrasena = "Abc1*"
        };
        await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);

        orden.Should().Equal("contrasena", "datos", "guardar");
    }

    [Fact]
    public async Task KeycloakFalla_NoLlamaGuardarCambiosAsync()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);
        _proveedor.Setup(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Keycloak caído"));

        var dto = new ModificarParticipanteSolicitudDto { Correo = "nuevo@umbral.com" };
        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);
        await accion.Should().ThrowAsync<InvalidOperationException>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SoloTelefono_GuardaPeroNoTocaKeycloak()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        var dto = new ModificarParticipanteSolicitudDto
        {
            DatosContacto = new DatosContactoDto { Telefono = "04149999999" }
        };
        await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);

        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CambiaCorreo_LlamaActualizarUsuarioAsync()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        var dto = new ModificarParticipanteSolicitudDto { Correo = "nuevo@umbral.com" };
        await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);

        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            "kc-participante",
            It.Is<DatosActualizacionUsuarioIdentidad>(d => d.Correo == "nuevo@umbral.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidadorUnicidadRecibeIdDelParticipanteAutenticado()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        ModificarParticipanteComando? capturado = null;
        _validadorUnicidad
            .Setup(v => v.ValidarAsync(
                It.IsAny<ModificarParticipanteComando>(), It.IsAny<CancellationToken>()))
            .Callback<ModificarParticipanteComando, CancellationToken>((c, _) => capturado = c)
            .ReturnsAsync(ResultadoValidacion.Exitoso());

        await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", new ModificarParticipanteSolicitudDto { Correo = "x@y.com" }),
            CancellationToken.None);

        capturado.Should().NotBeNull();
        capturado!.IdParticipanteActual.Should().Be(p.Id);
    }

    // ============================================================
    // Alias (HU10).
    // ============================================================

    [Fact]
    public async Task SoloAlias_NoLlamaKeycloak_PeroGuardaBase()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        var dto = new ModificarParticipanteSolicitudDto { Alias = "alias_nuevo" };
        var resultado = await CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeTrue();
        resultado.CamposActualizados.Should().Contain("alias");
        // Sin llamadas a Keycloak: alias vive solo en PostgreSQL.
        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _proveedor.Verify(p => p.CambiarContrasenaAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        // Sí persiste (ActualizarAsync + GuardarCambios).
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AliasYCorreo_KeycloakFalla_NoGuardaAlias()
    {
        // Si el alias cambia pero Keycloak rechaza el correo, NO debemos
        // confirmar SaveChanges; el alias preparado en el contexto EF queda
        // descartado al disponer del scope.
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);
        _proveedor
            .Setup(p => p.ActualizarUsuarioAsync(
                It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak rechazó el correo"));

        var dto = new ModificarParticipanteSolicitudDto
        {
            Alias = "alias_nuevo",
            Correo = "nuevo@umbral.com"
        };
        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarParticipanteComando("kc-x", dto), CancellationToken.None);
        await accion.Should().ThrowAsync<InvalidOperationException>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class LoggerCapturador<T> : ILogger<T>
    {
        public List<string> Mensajes { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instancia;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Mensajes.Add(formatter(state, exception));
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instancia = new();
            public void Dispose() { }
        }
    }
}
