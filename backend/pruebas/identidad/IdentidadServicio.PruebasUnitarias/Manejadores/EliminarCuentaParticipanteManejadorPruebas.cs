using IdentidadServicio.Aplicacion.Comandos.EliminarCuentaParticipante;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU11 — pruebas del coordinador del caso de uso de eliminación permanente
// de la cuenta del Participante autenticado. Cubrimos:
//  * identificación por sub del token;
//  * regla de dominio "solo Activo puede eliminarse";
//  * orden de eliminación EF → Keycloak → SaveChanges;
//  * comportamiento ante fallos de Keycloak;
//  * idempotencia ante 404 de Keycloak (la implementación del proveedor
//    no lanza, sólo lo verificamos a nivel de manejador);
//  * la respuesta no contiene datos personales.
public class EliminarCuentaParticipanteManejadorPruebas
{
    private readonly Mock<IRepositorioParticipantes> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();

    public EliminarCuentaParticipanteManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _proveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private EliminarCuentaParticipanteManejador CrearManejador()
        => new(
            _repositorio.Object,
            _unidad.Object,
            _proveedor.Object,
            Mock.Of<IRegistroLogsAplicacion>());

    private void EncolarParticipante(string idKeycloak, Participante p)
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdKeycloakAsync(idKeycloak, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);
    }

    [Fact]
    public async Task SinSubEnToken_LanzaDatosInvalidos()
    {
        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarCuentaParticipanteComando(string.Empty), CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
        _repositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _proveedor.Verify(
            p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoExisteParticipanteParaElSub_LanzaDatosInvalidos()
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdKeycloakAsync("kc-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Participante?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarCuentaParticipanteComando("kc-x"), CancellationToken.None);

        (await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>())
            .Which.Message.Should().Contain("No existe un Participante");
        _proveedor.Verify(
            p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParticipanteInactivo_LanzaCuentaDesactivada()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        p.Desactivar();
        EncolarParticipante("kc-x", p);

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarCuentaParticipanteComando("kc-x"), CancellationToken.None);

        await accion.Should().ThrowAsync<CuentaDesactivadaExcepcion>();
        _repositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _proveedor.Verify(
            p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParticipanteActivo_EliminaEnOrdenCorrecto()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);

        var orden = new List<string>();
        _repositorio
            .Setup(r => r.EliminarAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()))
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
            new EliminarCuentaParticipanteComando("kc-x"), CancellationToken.None);

        orden.Should().Equal("eliminar-bd", "eliminar-keycloak", "guardar");
        resultado.Eliminada.Should().BeTrue();
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UsaSubDelTokenParaLlamarAKeycloak()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-participante-9", p);

        await CrearManejador().Handle(
            new EliminarCuentaParticipanteComando("kc-participante-9"),
            CancellationToken.None);

        _proveedor.Verify(p => p.EliminarUsuarioAsync(
            "kc-participante-9", It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.EliminarAsync(
            It.Is<Participante>(x => x.Id == p.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task KeycloakFalla_NoLlamaGuardarCambiosAsync()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);
        _proveedor
            .Setup(pr => pr.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak caído"));

        Func<Task> accion = () => CrearManejador().Handle(
            new EliminarCuentaParticipanteComando("kc-x"), CancellationToken.None);
        await accion.Should().ThrowAsync<InvalidOperationException>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task KeycloakRespondeNotFound_Idempotente_GuardaCambios()
    {
        // El proveedor real trata 404 como éxito (la cuenta ya no existe en
        // Keycloak), así que EliminarUsuarioAsync devuelve sin excepción.
        // Verificamos que el manejador, al recibir esa "respuesta OK", sigue
        // confirmando la transacción en PostgreSQL.
        var p = UsuariosDePrueba.NuevoParticipante();
        EncolarParticipante("kc-x", p);
        _proveedor
            .Setup(pr => pr.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var resultado = await CrearManejador().Handle(
            new EliminarCuentaParticipanteComando("kc-x"), CancellationToken.None);

        resultado.Eliminada.Should().BeTrue();
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Respuesta_NoExponeDatosPersonalesDelParticipante()
    {
        var p = UsuariosDePrueba.NuevoParticipante(
            nombreUsuario: "participante01",
            correo: "pablo@umbral.com",
            nombre: "Pablo",
            apellido: "Participante",
            alias: "pablito");
        EncolarParticipante("kc-x", p);

        var resultado = await CrearManejador().Handle(
            new EliminarCuentaParticipanteComando("kc-x"), CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(resultado);
        json.Should().NotContain("Pablo");
        json.Should().NotContain("pablo@umbral.com");
        json.Should().NotContain("pablito");
        json.Should().NotContain("participante01");
    }
}
