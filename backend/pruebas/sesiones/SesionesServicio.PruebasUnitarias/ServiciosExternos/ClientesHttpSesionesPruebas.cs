using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.ServiciosExternos;

namespace SesionesServicio.PruebasUnitarias.ServiciosExternos;

public class ClientesHttpSesionesPruebas
{
    [Fact]
    public async Task ClienteIdentidadParticipantes_EnviaIdsDistintosConToken()
    {
        var id = Guid.NewGuid();
        var handler = new HandlerHttp(_ => JsonResponse(
            $"[{{\"id\":\"{id}\",\"nombre\":\"Ana\",\"apellido\":\"Paz\",\"alias\":\"ana\"}}]"));
        var cliente = CrearParticipantes(handler, token: "token-identidad");

        var resultado = await cliente.ObtenerParticipantesPorIdsAsync(
            new[] { id, Guid.Empty, id }, CancellationToken.None);

        resultado.Should().ContainKey(id);
        resultado[id].Alias.Should().Be("ana");
        var solicitud = handler.Solicitudes.Should().ContainSingle().Subject;
        solicitud.Method.Should().Be(HttpMethod.Post);
        solicitud.RequestUri!.ToString().Should().Be("https://identidad.test/api/usuarios/participantes/por-ids");
        solicitud.Headers.Authorization!.Parameter.Should().Be("token-identidad");
        handler.Cuerpos.Single().Should().Contain(id.ToString()).And.NotContain(Guid.Empty.ToString());
    }

    [Fact]
    public async Task ClienteIdentidadParticipantes_SinUrlOIds_DevuelveVacio()
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var sinUrl = await CrearParticipantes(handler, url: "")
            .ObtenerParticipantesPorIdsAsync(new[] { Guid.NewGuid() }, CancellationToken.None);
        var sinIds = await CrearParticipantes(handler)
            .ObtenerParticipantesPorIdsAsync(Array.Empty<Guid>(), CancellationToken.None);

        sinUrl.Should().BeEmpty();
        sinIds.Should().BeEmpty();
        handler.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    public async Task ClienteIdentidadUsuarios_FiltraAdministradoresYEsAdministrador()
    {
        var admin = Guid.NewGuid();
        var handler = new HandlerHttp(_ => JsonResponse(
            $"{{\"administradoresIds\":[\"{admin}\"]}}"));
        var cliente = CrearUsuarios(handler, token: "token-admin");

        var filtrados = await cliente.FiltrarAdministradoresAsync(
            new[] { admin, Guid.NewGuid() }, CancellationToken.None);
        var esAdmin = await cliente.EsAdministradorAsync(admin, CancellationToken.None);

        filtrados.Should().ContainSingle().Which.Should().Be(admin);
        esAdmin.Should().BeTrue();
        handler.Solicitudes.Should().HaveCount(2);
        handler.Solicitudes[0].Headers.Authorization!.Parameter.Should().Be("token-admin");
    }

    [Fact]
    public async Task ClienteIdentidadUsuarios_ErrorHttp_DevuelveVacio()
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var cliente = CrearUsuarios(handler);

        var filtrados = await cliente.FiltrarAdministradoresAsync(
            new[] { Guid.NewGuid() }, CancellationToken.None);

        filtrados.Should().BeEmpty();
    }

    [Fact]
    public async Task ClienteJuegosTrivia_ObtieneTriviaYVerificaRespuesta()
    {
        var triviaId = Guid.NewGuid();
        var preguntaId = Guid.NewGuid();
        var opcionId = Guid.NewGuid();
        var handler = new HandlerHttp(req =>
            req.Method == HttpMethod.Get
                ? JsonResponse($"{{\"id\":\"{triviaId}\",\"tiempoLimitePorPregunta\":10,\"preguntas\":[]}}")
                : JsonResponse("{\"esCorrecta\":true,\"puntajeBase\":50,\"tiempoLimiteSegundos\":10}"));
        var cliente = CrearTrivia(handler, token: "token-juegos");

        var trivia = await cliente.ObtenerTriviaParticipanteAsync(triviaId, CancellationToken.None);
        var verificacion = await cliente.VerificarRespuestaAsync(
            triviaId, preguntaId, opcionId, CancellationToken.None);

        trivia!.Id.Should().Be(triviaId);
        verificacion!.EsCorrecta.Should().BeTrue();
        handler.Solicitudes.Should().HaveCount(2);
        handler.Solicitudes.Should().OnlyContain(s => s.Headers.Authorization!.Parameter == "token-juegos");
        handler.Cuerpos.Last().Should().Contain(opcionId.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ClienteJuegosTrivia_NotFoundOForbidden_DevuelveNull(HttpStatusCode status)
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(status));
        var cliente = CrearTrivia(handler);

        var trivia = await cliente.ObtenerTriviaParticipanteAsync(Guid.NewGuid(), CancellationToken.None);

        trivia.Should().BeNull();
    }

    [Fact]
    public async Task ClienteBusquedaTesoro_ObtieneBusquedaYValidaQr()
    {
        var busquedaId = Guid.NewGuid();
        var handler = new HandlerHttp(req =>
            req.Method == HttpMethod.Get
                ? JsonResponse($"{{\"id\":\"{busquedaId}\",\"puntaje\":80}}")
                : JsonResponse("{\"esValida\":true}"));
        var cliente = CrearTesoro(handler, token: "token-tesoro");

        var busqueda = await cliente.ObtenerBusquedaParticipanteAsync(busquedaId, CancellationToken.None);
        var valida = await cliente.ValidarCodigoQrAsync(busquedaId, "QR-1", CancellationToken.None);

        busqueda!.Id.Should().Be(busquedaId);
        busqueda.Puntaje.Should().Be(80);
        valida.Should().BeTrue();
        handler.Solicitudes.Should().OnlyContain(s => s.Headers.Authorization!.Parameter == "token-tesoro");
        handler.Cuerpos.Last().Should().Contain("QR-1");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ClienteBusquedaTesoro_NotFoundOForbidden_DevuelveNull(HttpStatusCode status)
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(status));
        var cliente = CrearTesoro(handler);

        var resultado = await cliente.ValidarCodigoQrAsync(Guid.NewGuid(), "QR", CancellationToken.None);

        resultado.Should().BeNull();
    }

    private static ClienteIdentidadParticipantes CrearParticipantes(
        HandlerHttp handler,
        string url = "https://identidad.test",
        string? token = null)
        => new(
            new HttpClient(handler),
            Options.Create(new OpcionesIdentidadServicio { Url = url }),
            Mock.Of<IPropagadorTokenActual>(p => p.ObtenerTokenActual() == token),
            NullLogger<ClienteIdentidadParticipantes>.Instance);

    private static ClienteIdentidadUsuariosHttp CrearUsuarios(
        HandlerHttp handler,
        string url = "https://identidad.test",
        string? token = null)
        => new(
            new HttpClient(handler),
            Options.Create(new OpcionesIdentidadServicio { Url = url }),
            Mock.Of<IPropagadorTokenActual>(p => p.ObtenerTokenActual() == token),
            NullLogger<ClienteIdentidadUsuariosHttp>.Instance);

    private static ClienteJuegosTriviaHttp CrearTrivia(
        HandlerHttp handler,
        string url = "https://juegos.test",
        string? token = null)
        => new(
            new HttpClient(handler),
            Options.Create(new OpcionesJuegosServicio { Url = url }),
            Mock.Of<IPropagadorTokenActual>(p => p.ObtenerTokenActual() == token));

    private static ClienteBusquedaTesoroHttp CrearTesoro(
        HandlerHttp handler,
        string url = "https://juegos.test",
        string? token = null)
        => new(
            new HttpClient(handler),
            Options.Create(new OpcionesJuegosServicio { Url = url }),
            Mock.Of<IPropagadorTokenActual>(p => p.ObtenerTokenActual() == token));

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class HandlerHttp : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<HttpRequestMessage> Solicitudes { get; } = new();
        public List<string> Cuerpos { get; } = new();

        public HandlerHttp(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                Cuerpos.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            Solicitudes.Add(request);
            return _responder(request);
        }
    }
}
