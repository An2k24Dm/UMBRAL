namespace RankingServicio.PruebasIntegracion;

// Autorización (401/403), manejo de errores (500), rutas inexistentes (404) y
// propagación de X-Correlation-Id por el middleware de logging.
public sealed class RankingSeguridadYObservabilidadPruebas
    : IClassFixture<FabricaApiRankingPruebas>
{
    private readonly FabricaApiRankingPruebas _fabrica;

    public RankingSeguridadYObservabilidadPruebas(FabricaApiRankingPruebas fabrica)
        => _fabrica = fabrica;

    private HttpClient ClienteConRol(string rol)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
        return cliente;
    }

    [Fact]
    public async Task Participantes_SinAutenticacion_Devuelve401()
    {
        var cliente = _fabrica.CreateClient(); // sin cabecera de rol

        var respuesta = await cliente.GetAsync(
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionConDatos}/participantes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Global_SinAutenticacion_Devuelve401()
    {
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/ranking/global?top=5");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task Global_ConRolNoParticipante_Devuelve403(string rol)
    {
        var cliente = ClienteConRol(rol);

        var respuesta = await cliente.GetAsync("/api/ranking/global?top=5");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Global_ConParticipante_ErrorInternoSeTraduceA500Controlado()
    {
        // El ranking global usa SQL crudo específico de PostgreSQL (SqlQuery), no
        // soportado por SQLite. La excepción resultante debe traducirse al envelope
        // 500 del ManejadorErroresMiddleware. Esta prueba verifica ese camino real
        // de error (no un happy-path del ranking global).
        var cliente = ClienteConRol("Participante");

        var respuesta = await cliente.GetAsync("/api/ranking/global?top=5");

        respuesta.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().Contain("ERROR_INTERNO");
    }

    [Fact]
    public async Task RutaInexistente_Devuelve404()
    {
        var cliente = ClienteConRol("Participante");

        var respuesta = await cliente.GetAsync("/api/ranking/no-existe");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Respuesta_PropagaCorrelationIdRecibido()
    {
        var cliente = ClienteConRol("Participante");
        var solicitud = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionConDatos}/participantes");
        solicitud.Headers.Add("X-Correlation-Id", "corr-abc-123");

        var respuesta = await cliente.SendAsync(solicitud);

        respuesta.Headers.GetValues("X-Correlation-Id")
            .Should().ContainSingle().Which.Should().Be("corr-abc-123");
    }

    [Fact]
    public async Task Respuesta_GeneraCorrelationIdCuandoNoSeEnvia()
    {
        var cliente = ClienteConRol("Participante");

        var respuesta = await cliente.GetAsync(
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionConDatos}/participantes");

        respuesta.Headers.TryGetValues("X-Correlation-Id", out var valores).Should().BeTrue();
        valores!.Single().Should().NotBeNullOrWhiteSpace();
    }
}
