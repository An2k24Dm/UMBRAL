using System.Net;
using System.Net.Http.Json;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasIntegracion;

public sealed class MisionesEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly HttpClient _cliente;

    public MisionesEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    private static CrearMisionDto DtoMision(string? nombre = null) => new()
    {
        Nombre = nombre ?? "Misión-" + Guid.NewGuid().ToString("N")[..8],
        Descripcion = "Descripción de prueba para la misión.",
        Dificultad = 1
    };

    private HttpRequestMessage Con(HttpMethod metodo, string url, string rol, object? cuerpo = null)
    {
        var req = new HttpRequestMessage(metodo, url);
        req.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        if (cuerpo is not null)
            req.Content = JsonContent.Create(cuerpo);
        return req;
    }

    // ===== AUTH =====

    [Fact]
    public async Task PostMisiones_SinToken_Retorna401()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/juegos/misiones", DtoMision());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostMisiones_ConTokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Operador", DtoMision()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMisionesBorrador_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync("/api/juegos/misiones/borrador");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMisionesBorrador_ConTokenParticipante_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/misiones/borrador", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMisionesActivas_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync("/api/juegos/misiones/activas");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===== CRUD MISIÓN =====

    [Fact]
    public async Task PostMisiones_Administrador_DatosValidos_Retorna201ConId()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<IdRespuesta>();
        cuerpo!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostMisiones_Administrador_NombreVacio_Retorna400()
    {
        var dto = new CrearMisionDto { Nombre = "", Descripcion = "Desc", Dificultad = 1 };
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMisionesBorrador_Administrador_ListaVaciaONo_Retorna200()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/misiones/borrador", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<MisionResumenDto>>();
        lista.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMisionesActivas_Operador_Retorna200()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/misiones/activas", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMisionDetalle_Inexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(
            Con(HttpMethod.Get, $"/api/juegos/misiones/{Guid.NewGuid()}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMisionDetalle_Existente_Retorna200ConDatos()
    {
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var id = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/misiones/{id}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<MisionDetalleDto>();
        detalle!.Id.Should().Be(id);
    }

    [Fact]
    public async Task PatchMision_Administrador_DatosValidos_Retorna204()
    {
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var id = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var dto = new ModificarMisionDto { Nombre = "Nombre actualizado", Descripcion = "Desc actualizada", Dificultad = 2 };
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/misiones/{id}", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMision_DesactivarEnBorrador_Retorna422()
    {
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var id = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/misiones/{id}", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteMision_Eliminar_EnBorrador_Retorna204()
    {
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var id = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/misiones/{id}/eliminar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== ACTIVAR =====

    [Fact]
    public async Task PatchActivarMision_SinEtapas_Retorna422()
    {
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var id = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/misiones/{id}/activar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ===== ETAPAS =====

    private async Task<Guid> CrearTriviaActivaAsync()
    {
        var dto = new CrearTriviaDto
        {
            Nombre = "Trivia-Etapa-" + Guid.NewGuid().ToString("N")[..8],
            Descripcion = "Trivia activa para etapa de misión.",
            TiempoLimitePorPregunta = 30
        };
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/trivias", "Administrador", dto));
        var triviaId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var pregunta = new AgregarPreguntaDto
        {
            Enunciado = "¿Pregunta de etapa?",
            PuntajeAsignado = 10,
            TiempoEstimado = 15,
            Opciones = new List<OpcionDto>
            {
                new() { Texto = "Sí", EsCorrecta = true },
                new() { Texto = "No", EsCorrecta = false }
            }
        };
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/trivias/{triviaId}/preguntas", "Administrador", pregunta));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/trivias/{triviaId}/activar", "Administrador"));

        return triviaId;
    }

    [Fact]
    public async Task PostEtapa_Administrador_ConTriviaActiva_Retorna201()
    {
        var triviaId = await CrearTriviaActivaAsync();
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var misionId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var dto = new AgregarEtapaDto { TipoModoDeJuego = 0, ModoDeJuegoId = triviaId };
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/misiones/{misionId}/etapas", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<IdRespuesta>();
        cuerpo!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostEtapa_SinToken_Retorna401()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/juegos/misiones/{Guid.NewGuid()}/etapas",
            new AgregarEtapaDto { TipoModoDeJuego = 0, ModoDeJuegoId = Guid.NewGuid() });
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteEtapa_Administrador_Retorna204()
    {
        var triviaId = await CrearTriviaActivaAsync();
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var misionId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var crearEtapa = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/misiones/{misionId}/etapas", "Administrador",
            new AgregarEtapaDto { TipoModoDeJuego = 0, ModoDeJuegoId = triviaId }));
        var etapaId = (await crearEtapa.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/misiones/{misionId}/etapas/{etapaId}", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMisionParticipante_Existente_Retorna404_CuandoEnBorrador()
    {
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/misiones", "Administrador", DtoMision()));
        var id = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/misiones/participante/{id}", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class IdRespuesta
    {
        public Guid Id { get; set; }
    }
}
