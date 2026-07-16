using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasIntegracion;

public sealed class TriviasEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly HttpClient _cliente;

    public TriviasEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    private static CrearTriviaDto DtoTrivia(string? nombre = null) => new()
    {
        Nombre = nombre ?? "Trivia-" + Guid.NewGuid().ToString("N")[..8],
        Descripcion = "Descripción de la trivia de prueba.",
        TiempoLimitePorPregunta = 30
    };

    private static AgregarPreguntaDto DtoPregunta() => new()
    {
        Enunciado = "¿Cuánto es 2 + 2?",
        PuntajeAsignado = 100,
        TiempoEstimado = 15,
        Opciones = new List<OpcionDto>
        {
            new() { Texto = "3", EsCorrecta = false },
            new() { Texto = "4", EsCorrecta = true },
            new() { Texto = "5", EsCorrecta = false }
        }
    };

    private HttpRequestMessage Con(HttpMethod metodo, string url, string rol, object? cuerpo = null)
    {
        var req = new HttpRequestMessage(metodo, url);
        req.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        if (cuerpo is not null)
            req.Content = JsonContent.Create(cuerpo);
        return req;
    }

    private async Task<Guid> CrearTriviaAsync(string? nombre = null)
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/trivias", "Administrador", DtoTrivia(nombre)));
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;
    }

    // ===== AUTH =====

    [Fact]
    public async Task PostTrivias_SinToken_Retorna401()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/juegos/trivias", DtoTrivia());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTrivias_ConTokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/trivias", "Operador", DtoTrivia()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTriviaBorrador_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync("/api/juegos/trivias/borrador");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTriviaBorrador_ConTokenParticipante_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/trivias/borrador", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ===== CRUD TRIVIA =====

    [Fact]
    public async Task PostTrivias_Administrador_DatosValidos_Retorna201ConId()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/trivias", "Administrador", DtoTrivia()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<IdRespuesta>();
        cuerpo!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostTrivias_NombreDuplicado_Retorna422()
    {
        var nombre = "Trivia-Unica-" + Guid.NewGuid().ToString("N")[..6];
        await CrearTriviaAsync(nombre);

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/trivias", "Administrador", DtoTrivia(nombre)));
        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetTriviaDetalle_Existente_Operador_Retorna200()
    {
        var id = await CrearTriviaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/trivias/{id}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<TriviaDetalleDto>();
        detalle!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetTriviaDetalle_Inexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/trivias/{Guid.NewGuid()}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTriviasBorrador_Administrador_Retorna200ConLista()
    {
        await CrearTriviaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/trivias/borrador", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<TriviaResumenDto>>();
        lista.Should().NotBeNull();
        lista!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTriviasActivas_Operador_Retorna200()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/trivias/activas", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutTrivia_Administrador_DatosValidos_Retorna204()
    {
        var id = await CrearTriviaAsync();
        var dto = new ModificarTriviaDto
        {
            NuevoNombre = "Trivia-Modificada-" + Guid.NewGuid().ToString("N")[..6],
            NuevaDescripcion = "Nueva descripción.",
            NuevoTiempoLimitePorPregunta = 45
        };
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Put, $"/api/juegos/trivias/{id}", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTrivia_Eliminar_EnBorrador_Retorna204()
    {
        var id = await CrearTriviaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/trivias/{id}/eliminar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== ACTIVAR =====

    [Fact]
    public async Task PatchActivarTrivia_SinPreguntas_Retorna422()
    {
        var id = await CrearTriviaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/trivias/{id}/activar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PatchActivarTrivia_ConPreguntas_Retorna204()
    {
        var id = await CrearTriviaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{id}/preguntas", "Administrador", DtoPregunta()));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/trivias/{id}/activar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTrivia_DesactivarActiva_Retorna204()
    {
        var id = await CrearTriviaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{id}/preguntas", "Administrador", DtoPregunta()));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/trivias/{id}/activar", "Administrador"));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/trivias/{id}", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== PREGUNTAS =====

    [Fact]
    public async Task PostPregunta_Administrador_DatosValidos_Retorna201()
    {
        var id = await CrearTriviaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{id}/preguntas", "Administrador", DtoPregunta()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<IdRespuesta>();
        cuerpo!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostPregunta_SinToken_Retorna401()
    {
        var respuesta = await _cliente.PostAsJsonAsync($"/api/juegos/trivias/{Guid.NewGuid()}/preguntas", DtoPregunta());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutPregunta_Administrador_Retorna204()
    {
        var triviaId = await CrearTriviaAsync();
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{triviaId}/preguntas", "Administrador", DtoPregunta()));
        var preguntaId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var dto = new ModificarPreguntaDto
        {
            NuevoEnunciado = "¿Cuánto es 3 + 3?",
            NuevoTiempoEstimado = 20,
            NuevasOpciones = new List<OpcionDto>
            {
                new() { Texto = "5", EsCorrecta = false },
                new() { Texto = "6", EsCorrecta = true }
            }
        };
        var respuesta = await _cliente.SendAsync(
            Con(HttpMethod.Put, $"/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePregunta_Administrador_Retorna204()
    {
        var triviaId = await CrearTriviaAsync();
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{triviaId}/preguntas", "Administrador", DtoPregunta()));
        var preguntaId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(
            Con(HttpMethod.Delete, $"/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== VISTA PARTICIPANTE =====

    [Fact]
    public async Task GetTriviaParticipante_Existente_Retorna200()
    {
        var id = await CrearTriviaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{id}/preguntas", "Administrador", DtoPregunta()));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/trivias/{id}/activar", "Administrador"));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/trivias/{id}/participante", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<TriviaParticipanteDto>();
        dto!.Id.Should().Be(id);
        dto.Preguntas.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTriviaParticipante_Inexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/trivias/{Guid.NewGuid()}/participante", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class IdRespuesta { public Guid Id { get; set; } }
}
