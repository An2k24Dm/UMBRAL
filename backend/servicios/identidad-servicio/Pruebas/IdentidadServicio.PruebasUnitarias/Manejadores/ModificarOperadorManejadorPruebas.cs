using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU09 — pruebas unitarias del manejador de modificación parcial de Operador.
// Cubren los casos requeridos por la HU: actualización campo a campo,
// preservación de Estado/Rol/FechaRegistro, rechazo de operador inexistente,
// rechazo de correo duplicado en otro usuario, no-llamada a Keycloak cuando
// no aplica y sincronización cuando sí aplica.
public class ModificarOperadorManejadorPruebas
{
    private readonly Mock<IRepositorioOperadores> _repositorio = new();
    private readonly Mock<IRepositorioUnicidadUsuario> _unicidad = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaRegistroOriginal = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public ModificarOperadorManejadorPruebas()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        // Por defecto, ActualizarAsync devuelve el idKeycloak conocido y la
        // unidad de trabajo confirma sin error.
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-operador");
        _unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static Operador OperadorOriginal(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        NombreUsuario.Crear("operador01"),
        Correo.Crear("operador@umbral.com"),
        EstadoUsuario.Activo,
        FechaRegistroOriginal,
        NombrePersona.Crear("Olivia", "Operadora"),
        DatosContacto.Crear("Av. Libertador, Caracas", "04141111111"),
        SexoPersona.Femenino,
        new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        "OP-001");

    private ModificarOperadorManejador CrearManejador()
    {
        var validador = new ValidadorModificarOperador(new ReglasValidacionUsuario(_reloj.Object));
        var fabrica = new FabricaEstrategiaMapeoPerfilUsuario(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilParticipante()
        });

        return new ModificarOperadorManejador(
            _repositorio.Object,
            _unicidad.Object,
            _unidad.Object,
            _proveedor.Object,
            validador,
            fabrica,
            NullLogger<ModificarOperadorManejador>.Instance);
    }

    private void EncolarOperador(Operador op) =>
        _repositorio.Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(op);

    [Fact]
    public async Task ActualizaSoloNombre_ConservaCorreoEstadoRolYFechaRegistro()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        Operador? capturado = null;
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .Callback<Operador, CancellationToken>((o, _) => capturado = o)
            .ReturnsAsync("kc-operador");

        var dto = new ModificarOperadorSolicitudDto { Nombre = "Olivia María" };
        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeTrue();
        resultado.CamposActualizados.Should().Contain("nombre");
        capturado!.NombrePersona.Nombre.Should().Be("Olivia María");
        capturado.NombrePersona.Apellido.Should().Be("Operadora");
        capturado.Correo.Valor.Should().Be("operador@umbral.com");
        capturado.Estado.Should().Be(EstadoUsuario.Activo);
        capturado.Rol.Should().Be(RolUsuario.Operador);
        capturado.FechaRegistro.Should().Be(FechaRegistroOriginal);
    }

    [Fact]
    public async Task ActualizaSoloCorreo_ConservaResto()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        Operador? capturado = null;
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .Callback<Operador, CancellationToken>((o, _) => capturado = o)
            .ReturnsAsync("kc-operador");

        var dto = new ModificarOperadorSolicitudDto { Correo = "nuevo.correo@umbral.com" };
        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeTrue();
        resultado.CamposActualizados.Should().BeEquivalentTo(new[] { "correo" });
        capturado!.Correo.Valor.Should().Be("nuevo.correo@umbral.com");
        capturado.NombrePersona.Nombre.Should().Be("Olivia");
        capturado.NombrePersona.Apellido.Should().Be("Operadora");
        capturado.Estado.Should().Be(EstadoUsuario.Activo);
        capturado.FechaRegistro.Should().Be(FechaRegistroOriginal);
    }

    [Fact]
    public async Task ActualizaVariosCampos_AplicaTodos()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        var dto = new ModificarOperadorSolicitudDto
        {
            Nombre = "Otra",
            Apellido = "Operadora",
            Sexo = "Otro",
            DatosContacto = new DatosContactoDto
            {
                Direccion = "Av. Bolívar, Maracay",
                Telefono = "04241234567"
            }
        };

        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeTrue();
        resultado.CamposActualizados.Should().Contain("nombre");
        resultado.CamposActualizados.Should().Contain("sexo");
        resultado.CamposActualizados.Should().Contain("datosContacto.direccion");
        resultado.CamposActualizados.Should().Contain("datosContacto.telefono");
        resultado.CamposActualizados.Should().NotContain("apellido"); // no cambió
    }

    [Fact]
    public async Task CorreoDuplicadoEnOtroUsuario_Rechaza()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        _unicidad
            .Setup(r => r.ExisteCorreoEnOtroUsuarioAsync(
                "ada@umbral.com", original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ModificarOperadorSolicitudDto { Correo = "ada@umbral.com" };

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        var excepcion = await accion.Should().ThrowAsync<ExcepcionValidacion>();
        excepcion.Which.Errores.Should().Contain(e => e.Campo == "correo");

        _repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MismoCorreoDelOperadorActual_NoRechaza()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        // El puerto de unicidad responde false porque excluye al propio usuario.
        _unicidad
            .Setup(r => r.ExisteCorreoEnOtroUsuarioAsync(
                "operador@umbral.com", original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new ModificarOperadorSolicitudDto { Correo = "operador@umbral.com" };
        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        // No hay cambios porque el correo coincide con el actual.
        resultado.HuboCambios.Should().BeFalse();
        _repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OperadorInexistente_Rechaza()
    {
        var id = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Operador?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(id, new ModificarOperadorSolicitudDto { Nombre = "Otra" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
        _repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UsuarioNoEsOperador_RepositorioDevuelveNull_Rechaza()
    {
        // El puerto ObtenerOperadorPorIdAsync ya garantiza devolver null
        // si el id no corresponde a un Operador. El manejador lo trata como
        // "no encontrado".
        var id = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Operador?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(id, new ModificarOperadorSolicitudDto { Correo = "x@x.com" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public async Task SinCambios_NoPersisteNiLlamaKeycloak()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        // Mismo correo, mismo nombre — no hay diff.
        var dto = new ModificarOperadorSolicitudDto
        {
            Correo = "operador@umbral.com",
            Nombre = "Olivia"
        };

        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeFalse();
        resultado.Mensaje.Should().Contain("No había cambios");
        _repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CambiaCorreo_LlamaKeycloakConCorreo()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        DatosActualizacionUsuarioIdentidad? capturado = null;
        _proveedor
            .Setup(p => p.ActualizarUsuarioAsync(
                It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, DatosActualizacionUsuarioIdentidad, CancellationToken>(
                (_, d, _) => capturado = d)
            .Returns(Task.CompletedTask);

        var dto = new ModificarOperadorSolicitudDto { Correo = "nuevo@umbral.com" };
        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            "kc-operador", It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Once);
        capturado.Should().NotBeNull();
        capturado!.Correo.Should().Be("nuevo@umbral.com");
        capturado.NombreUsuario.Should().BeNull();
        capturado.Nombre.Should().BeNull();
        capturado.Apellido.Should().BeNull();
    }

    [Fact]
    public async Task CambiaSoloTelefono_NoLlamaKeycloak()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        var dto = new ModificarOperadorSolicitudDto
        {
            DatosContacto = new DatosContactoDto { Telefono = "04149999999" }
        };

        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CambiaNombreUsuario_LlamaKeycloakConUsername()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        DatosActualizacionUsuarioIdentidad? capturado = null;
        _proveedor
            .Setup(p => p.ActualizarUsuarioAsync(
                It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, DatosActualizacionUsuarioIdentidad, CancellationToken>(
                (_, d, _) => capturado = d)
            .Returns(Task.CompletedTask);

        var dto = new ModificarOperadorSolicitudDto { NombreUsuario = "operador.dos" };
        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        capturado!.NombreUsuario.Should().Be("operador.dos");
        capturado.Correo.Should().BeNull();
    }
}
