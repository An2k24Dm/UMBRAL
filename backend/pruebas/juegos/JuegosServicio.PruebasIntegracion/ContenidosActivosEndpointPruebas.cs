using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasIntegracion;

public sealed class ContenidosActivosEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly HttpClient _cliente;
    private readonly FabricaApiPruebas _fabrica;

    public ContenidosActivosEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Con(HttpMethod metodo, string url, string rol)
    {
        var req = new HttpRequestMessage(metodo, url);
        req.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        return req;
    }

    private async Task<Guid> CrearTriviaActivaAsync()
    {
        var dto = new CrearTriviaDto
        {
            Nombre = "Trivia-Activa-" + Guid.NewGuid().ToString("N")[..8],
            Descripcion = "Trivia para pruebas de contenidos activos.",
            TiempoLimitePorPregunta = 30
        };
        var crear = new HttpRequestMessage(HttpMethod.Post, "/api/juegos/trivias");
        crear.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        crear.Content = JsonContent.Create(dto);
        var respCrear = await _cliente.SendAsync(crear);
        var id = (await respCrear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var pregunta = new AgregarPreguntaDto
        {
            Enunciado = "¿Pregunta de prueba?",
            PuntajeAsignado = 100,
            TiempoEstimado = 15,
            Opciones = new List<OpcionDto>
            {
                new() { Texto = "Correcta", EsCorrecta = true },
                new() { Texto = "Incorrecta", EsCorrecta = false }
            }
        };
        var addPregunta = new HttpRequestMessage(HttpMethod.Post, $"/api/juegos/trivias/{id}/preguntas");
        addPregunta.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        addPregunta.Content = JsonContent.Create(pregunta);
        await _cliente.SendAsync(addPregunta);

        var activar = new HttpRequestMessage(HttpMethod.Patch, $"/api/juegos/trivias/{id}/activar");
        activar.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        await _cliente.SendAsync(activar);

        return id;
    }

    [Fact]
    public async Task GetContenidoActivo_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync($"/api/juegos/contenidos-activos/Trivia/{Guid.NewGuid()}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContenidoActivo_ConParticipante_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/contenidos-activos/Trivia/{Guid.NewGuid()}", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetContenidoActivo_TriviaActiva_Operador_Retorna200ConNombre()
    {
        var triviaId = await CrearTriviaActivaAsync();

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/contenidos-activos/Trivia/{triviaId}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<ContenidoJuegoActivoDto>();
        dto!.Id.Should().Be(triviaId);
        dto.Nombre.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetContenidoActivo_Inexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/contenidos-activos/Trivia/{Guid.NewGuid()}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContenidoActivo_TipoJuegoDesconocido_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/contenidos-activos/ModoDesconocido/{Guid.NewGuid()}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class IdRespuesta { public Guid Id { get; set; } }
}
