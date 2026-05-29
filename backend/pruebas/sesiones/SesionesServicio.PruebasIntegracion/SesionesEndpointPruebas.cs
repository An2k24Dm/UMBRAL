using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

public sealed class SesionesEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;

    public SesionesEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private HttpClient ClienteConRol(string? rol)
    {
        var cliente = _fabrica.CreateClient();
        if (rol is not null)
        {
            cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
            cliente.DefaultRequestHeaders.Add(
                AuthHandlerPruebas.CabeceraIdKeycloak,
                "11111111-1111-1111-1111-111111111111");
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "token-de-prueba");
        }
        return cliente;
    }

    private static CrearSesionSolicitudDto SolicitudValida(
        Guid contenidoJuegoId,
        string tipoJuego = "Trivia",
        string modo = "Individual") => new()
    {
        Nombre = "Sesión piloto",
        TipoJuego = tipoJuego,
        ContenidoJuegoId = contenidoJuegoId,
        Modo = modo,
        FechaProgramada = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task POST_SinToken_Devuelve401()
    {
        var cliente = _fabrica.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones", SolicitudValida(FabricaApiPruebas.IdTriviaActiva));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_ConParticipante_Devuelve403()
    {
        var cliente = ClienteConRol("Participante");
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones", SolicitudValida(FabricaApiPruebas.IdTriviaActiva));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task POST_RolPermitido_ConTriviaActiva_Devuelve201YPersiste(string rol)
    {
        var cliente = ClienteConRol(rol);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();
        cuerpo!.Estado.Should().Be("Programada");
        cuerpo.TipoJuego.Should().Be("Trivia");
        cuerpo.ContenidoJuegoId.Should().Be(FabricaApiPruebas.IdTriviaActiva);
        cuerpo.CreadaPorUsuarioId.Should().NotBeEmpty();

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        var persistida = ctx.Sesiones.Should().ContainSingle(s => s.Id == cuerpo.Id).Subject;
        persistida.ContenidoJuegoId.Should().Be(FabricaApiPruebas.IdTriviaActiva);
        persistida.CreadaPorUsuarioId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_OperadorConBusquedaActiva_Devuelve201()
    {
        var cliente = ClienteConRol("Operador");
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdBusquedaActiva, "BusquedaTesoro", "Grupo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();
        cuerpo!.TipoJuego.Should().Be("BusquedaTesoro");
        cuerpo.Modo.Should().Be("Grupo");
        cuerpo.Estado.Should().Be("Programada");
        cuerpo.ContenidoJuegoId.Should().Be(FabricaApiPruebas.IdBusquedaActiva);
    }

    [Fact]
    public async Task POST_ConContenidoInactivo_DevuelveErrorControlado()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaInactiva));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_ConContenidoInexistente_DevuelveErrorControlado()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdContenidoInexistente));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_RespuestaJson_NoDebeIncluirCamposEliminados()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().NotContain("nombreContenido");
        json.Should().NotContain("creadaPorNombreUsuario");
    }

    [Fact]
    public async Task GET_SinToken_Devuelve401()
    {
        var cliente = _fabrica.CreateClient();
        var respuesta = await cliente.GetAsync("/api/sesiones");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ConParticipante_Devuelve403()
    {
        var cliente = ClienteConRol("Participante");
        var respuesta = await cliente.GetAsync("/api/sesiones");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task GET_ConRolPermitido_DevuelveListadoConCamposVisibles(string rol)
    {
        var clienteAdmin = ClienteConRol("Administrador");
        await clienteAdmin.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));

        var cliente = ClienteConRol(rol);
        var respuesta = await cliente.GetAsync("/api/sesiones");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<SesionListadoDto>>();
        lista!.Should().NotBeEmpty();
        var primera = lista[0];
        primera.Nombre.Should().NotBeNullOrWhiteSpace();
        primera.TipoJuego.Should().NotBeNullOrWhiteSpace();
        primera.Modo.Should().NotBeNullOrWhiteSpace();
        primera.Estado.Should().NotBeNullOrWhiteSpace();
        primera.FechaProgramada.Should().NotBe(default);
        primera.ContenidoJuegoId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_PorId_Existente_DevuelveDetalle()
    {
        var cliente = ClienteConRol("Administrador");
        var creada = await cliente.PostAsJsonAsync(
            "/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));
        var cuerpo = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();

        var respuesta = await cliente.GetAsync($"/api/sesiones/{cuerpo!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<SesionDetalleDto>();
        detalle!.Id.Should().Be(cuerpo.Id);
        detalle.Estado.Should().Be("Programada");
        detalle.ContenidoJuegoId.Should().Be(FabricaApiPruebas.IdTriviaActiva);
        detalle.CreadaPorUsuarioId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_PorId_NoExistente_Devuelve404()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.GetAsync($"/api/sesiones/{Guid.NewGuid()}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
