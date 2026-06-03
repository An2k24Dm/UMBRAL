using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

public sealed class SesionesEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly string IdAdministrador = "11111111-1111-1111-1111-111111111111";
    private static readonly string IdOperadorA = "22222222-2222-2222-2222-222222222222";
    private static readonly string IdOperadorB = "33333333-3333-3333-3333-333333333333";

    private readonly FabricaApiPruebas _fabrica;

    public SesionesEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private HttpClient ClienteConRol(string? rol, string? idKeycloak = null)
    {
        var cliente = _fabrica.CreateClient();
        if (rol is not null)
        {
            cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
            cliente.DefaultRequestHeaders.Add(
                AuthHandlerPruebas.CabeceraIdKeycloak,
                idKeycloak ?? IdAdministrador);
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
        // Fecha futura relativa al reloj real (en pruebas de
        // integración no se mockea IProveedorFechaHora). 30 días
        // alcanza para que la prueba no se vuelva flaky con el paso
        // del tiempo y para que la regla "FechaProgramada > ahora"
        // siempre se satisfaga.
        FechaProgramada = DateTime.UtcNow.AddDays(30)
    };

    // ---------------------------------------------------------------
    // POST /api/sesiones (HU33)
    // ---------------------------------------------------------------

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

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task POST_ConFechaProgramadaPasada_Devuelve400_YNoPersiste(string rol)
    {
        // Usamos una fábrica dedicada con BD InMemory aislada para
        // verificar exactamente que ESTA solicitud no agrega filas; la
        // fixture compartida puede tener sesiones de otras pruebas.
        await using var fabrica = new FabricaApiPruebas();
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
        cliente.DefaultRequestHeaders.Add(
            AuthHandlerPruebas.CabeceraIdKeycloak,
            "11111111-1111-1111-1111-111111111111");
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "token-de-prueba");

        var solicitud = SolicitudValida(FabricaApiPruebas.IdTriviaActiva);
        solicitud.FechaProgramada = DateTime.UtcNow.AddMinutes(-5);

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        // La regla es una política de dominio: el middleware la traduce
        // a `codigo: "SESION_INVALIDA"` con el mensaje exacto.
        cuerpo.Should().Contain("SESION_INVALIDA");
        cuerpo.Should().Contain("no puede programarse");

        using var alcance = fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        ctx.Sesiones.Should().BeEmpty();
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

    // ---------------------------------------------------------------
    // GET /api/sesiones — listado (HU34)
    // ---------------------------------------------------------------

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

    [Fact]
    public async Task GET_Administrador_VeTodasLasSesiones()
    {
        await using var fabrica = new FabricaApiPruebas();

        // Sembramos 3 sesiones: 1 admin, 1 operador A, 1 operador B.
        var admin = fabrica.CreateClient();
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "tok");
        (await admin.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var opA = fabrica.CreateClient();
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorA);
        opA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        (await opA.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var opB = fabrica.CreateClient();
        opB.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opB.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorB);
        opB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        (await opB.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var respuesta = await admin.GetAsync("/api/sesiones");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<SesionListadoDto>>();
        lista!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GET_Operador_SoloVeSusYDeAdmin()
    {
        await using var fabrica = new FabricaApiPruebas();

        var admin = fabrica.CreateClient();
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        (await admin.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var opA = fabrica.CreateClient();
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorA);
        opA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        (await opA.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var opB = fabrica.CreateClient();
        opB.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opB.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorB);
        opB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        (await opB.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var respuesta = await opA.GetAsync("/api/sesiones");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<SesionListadoDto>>();

        // Operador A ve: la del admin + la suya. NO la del operador B.
        lista!.Should().HaveCount(2);
        lista.Should().Contain(s => s.CreadaPorUsuarioId == Guid.Parse(IdAdministrador));
        lista.Should().Contain(s => s.CreadaPorUsuarioId == Guid.Parse(IdOperadorA));
        lista.Should().NotContain(s => s.CreadaPorUsuarioId == Guid.Parse(IdOperadorB));
    }

    [Fact]
    public async Task GET_FiltroPorTipoJuego_FuncionaParaAdministrador()
    {
        await using var fabrica = new FabricaApiPruebas();
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");

        (await cliente.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdBusquedaActiva, "BusquedaTesoro", "Grupo")))
            .EnsureSuccessStatusCode();

        var soloTrivia = await cliente.GetAsync("/api/sesiones?tipoJuego=Trivia");
        soloTrivia.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await soloTrivia.Content.ReadFromJsonAsync<List<SesionListadoDto>>();
        lista!.Should().OnlyContain(s => s.TipoJuego == "Trivia");
    }

    [Fact]
    public async Task GET_FiltroPorEstado_FuncionaParaAdministrador()
    {
        await using var fabrica = new FabricaApiPruebas();
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");

        (await cliente.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva))).EnsureSuccessStatusCode();

        var programadas = await cliente.GetAsync("/api/sesiones?estado=Programada");
        programadas.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await programadas.Content.ReadFromJsonAsync<List<SesionListadoDto>>();
        lista!.Should().OnlyContain(s => s.Estado == "Programada");
        lista.Should().NotBeEmpty();

        var finalizadas = await cliente.GetAsync("/api/sesiones?estado=Finalizada");
        finalizadas.StatusCode.Should().Be(HttpStatusCode.OK);
        (await finalizadas.Content.ReadFromJsonAsync<List<SesionListadoDto>>())!
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GET_FiltroInvalido_Devuelve400()
    {
        var cliente = ClienteConRol("Administrador");
        var r1 = await cliente.GetAsync("/api/sesiones?tipoJuego=Otro");
        r1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var r2 = await cliente.GetAsync("/api/sesiones?estado=Invento");
        r2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------
    // GET /api/sesiones/{id} — detalle (HU34/5.2)
    // ---------------------------------------------------------------

    [Fact]
    public async Task GETid_NoExistente_Devuelve404()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.GetAsync($"/api/sesiones/{Guid.NewGuid()}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GETid_Administrador_VeDetalleConTrivia()
    {
        await using var fabrica = new FabricaApiPruebas();
        var admin = fabrica.CreateClient();
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");

        var creada = await admin.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));
        var cuerpo = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();

        var respuesta = await admin.GetAsync($"/api/sesiones/{cuerpo!.Id}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var detalle = await respuesta.Content.ReadFromJsonAsync<SesionDetalleDto>();
        detalle!.Id.Should().Be(cuerpo.Id);
        detalle.Estado.Should().Be("Programada");
        detalle.Trivia.Should().NotBeNull();
        detalle.Trivia!.Preguntas.Should().NotBeEmpty();
        detalle.BusquedaTesoro.Should().BeNull();
    }

    [Fact]
    public async Task GETid_Administrador_VeDetalleConBusqueda()
    {
        await using var fabrica = new FabricaApiPruebas();
        var admin = fabrica.CreateClient();
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");

        var creada = await admin.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdBusquedaActiva, "BusquedaTesoro", "Grupo"));
        var cuerpo = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();

        var respuesta = await admin.GetAsync($"/api/sesiones/{cuerpo!.Id}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var detalle = await respuesta.Content.ReadFromJsonAsync<SesionDetalleDto>();
        detalle!.BusquedaTesoro.Should().NotBeNull();
        detalle.BusquedaTesoro!.Pistas.Should().NotBeEmpty();
        detalle.Trivia.Should().BeNull();
    }

    [Fact]
    public async Task GETid_Operador_NoVeSesionDeOtroOperador_Devuelve403()
    {
        await using var fabrica = new FabricaApiPruebas();

        var opB = fabrica.CreateClient();
        opB.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opB.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorB);
        opB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        var creada = await opB.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));
        var cuerpo = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();

        var opA = fabrica.CreateClient();
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorA);
        opA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");

        var respuesta = await opA.GetAsync($"/api/sesiones/{cuerpo!.Id}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GETid_Operador_VeSesionDeAdministrador()
    {
        await using var fabrica = new FabricaApiPruebas();

        var admin = fabrica.CreateClient();
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        admin.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdAdministrador);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");
        var creada = await admin.PostAsJsonAsync("/api/sesiones",
            SolicitudValida(FabricaApiPruebas.IdTriviaActiva));
        var cuerpo = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>();

        var opA = fabrica.CreateClient();
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        opA.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraIdKeycloak, IdOperadorA);
        opA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tok");

        var respuesta = await opA.GetAsync($"/api/sesiones/{cuerpo!.Id}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
