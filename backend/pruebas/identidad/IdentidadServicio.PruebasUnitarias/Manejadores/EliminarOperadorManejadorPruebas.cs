using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU13 — pruebas del coordinador del caso de uso de eliminación permanente
// de un Operador por un Administrador. Cubrimos:
//  * 404 controlado si el id no es Operador (no existe / es otro rol);
//  * orden BD → Keycloak → SaveChanges;
//  * fallos de Keycloak no confirman BD;
//  * uso del IdKeycloak correcto al hablar con el proveedor;
//  * idempotencia ante 404 de Keycloak (la implementación del proveedor
//    no lanza, sólo lo verificamos a nivel de manejador);
//  * la respuesta no contiene datos sensibles del Operador eliminado.
public class EliminarOperadorManejadorPruebas
{
    private readonly Mock<IRepositorioOperadores> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();

    public EliminarOperadorManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _proveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private EliminarOperadorManejador CrearManejador()
        => new(
            _repositorio.Object,
            _unidad.Object,
            _proveedor.Object,
            NullLogger<EliminarOperadorManejador>.Instance);

    private void EncolarOperador(Guid id, Operador op, string idKeycloak = "kc-operador")
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);
        _repositorio
            .Setup(r => r.ObtenerIdKeycloakAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(idKeycloak);
    }

    [Fact]
    public async Task OperadorNoExiste_LanzaDatosInvalidos_YNoTocaKeycloakNiBd()
    {
        var idInexistente = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Operador?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarOperadorComando(idInexistente), CancellationToken.None);

        (await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>())
            .Which.Message.Should().Contain("No existe un Operador");
        _repositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _proveedor.Verify(
            p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IdCorrespondeAAdministradorOParticipante_RepositorioDevuelveNull_LanzaDatosInvalidos()
    {
        // ObtenerPorIdAsync ya filtra por rol Operador: si el id corresponde
        // a un Administrador o Participante, devuelve null. Verificamos que
        // el manejador NO intenta eliminar nada en ese caso.
        var id = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Operador?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarOperadorComando(id), CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
        _repositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _proveedor.Verify(
            p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OperadorSinIdKeycloak_NoLlamaKeycloakNiGuardaCambios()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);
        _repositorio
            .Setup(r => r.ObtenerIdKeycloakAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarOperadorComando(op.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        _repositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _proveedor.Verify(
            p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OperadorValido_EliminaEnOrdenCorrecto()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        EncolarOperador(op.Id, op, idKeycloak: "kc-op-001");

        var orden = new List<string>();
        _repositorio
            .Setup(r => r.EliminarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("eliminar-bd"))
            .Returns(Task.CompletedTask);
        _proveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("eliminar-keycloak"))
            .Returns(Task.CompletedTask);
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("guardar"))
            .Returns(Task.CompletedTask);

        var resultado = await CrearManejador().Handle(
            new EliminarOperadorComando(op.Id), CancellationToken.None);

        orden.Should().Equal("eliminar-bd", "eliminar-keycloak", "guardar");
        resultado.Eliminado.Should().BeTrue();
        resultado.IdOperador.Should().Be(op.Id);
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UsaIdKeycloakDelRepositorio_NoAdivina()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        EncolarOperador(op.Id, op, idKeycloak: "kc-real-de-bd");

        await CrearManejador().Handle(
            new EliminarOperadorComando(op.Id), CancellationToken.None);

        _proveedor.Verify(p => p.EliminarUsuarioAsync(
            "kc-real-de-bd", It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.EliminarAsync(
            It.Is<Operador>(x => x.Id == op.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task KeycloakFalla_NoLlamaGuardarCambiosAsync()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        EncolarOperador(op.Id, op);
        _proveedor
            .Setup(pr => pr.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak caído"));

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarOperadorComando(op.Id), CancellationToken.None);
        await accion.Should().ThrowAsync<InvalidOperationException>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task KeycloakRespondeOk_GuardaCambios()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        EncolarOperador(op.Id, op);
        _proveedor
            .Setup(pr => pr.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var resultado = await CrearManejador().Handle(
            new EliminarOperadorComando(op.Id), CancellationToken.None);

        resultado.Eliminado.Should().BeTrue();
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Respuesta_NoExponeDatosPersonalesDelOperador()
    {
        var op = UsuariosDePrueba.NuevoOperador(
            nombreUsuario: "olivia_op",
            correo: "olivia@umbral.com",
            nombre: "Olivia",
            apellido: "Operadora",
            codigoOperador: "OP-XYZ");
        EncolarOperador(op.Id, op);

        var resultado = await CrearManejador().Handle(
            new EliminarOperadorComando(op.Id), CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(resultado);
        // Sólo debe contener id, flag y mensaje. Ni correo, ni nombre, ni
        // código del Operador eliminado.
        json.Should().NotContain("Olivia");
        json.Should().NotContain("olivia@umbral.com");
        json.Should().NotContain("olivia_op");
        json.Should().NotContain("OP-XYZ");
    }
}
