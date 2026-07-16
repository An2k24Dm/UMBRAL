using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasIntegracion;

public sealed class BusquedasEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly HttpClient _cliente;

    public BusquedasEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    private static CrearBusquedaTesoroDto DtoBusqueda(string? nombre = null) => new()
    {
        Nombre = nombre ?? "Busqueda-" + Guid.NewGuid().ToString("N")[..8],
        Descripcion = "Descripción de la búsqueda del tesoro de prueba.",
        Tiempo = 30,
        Puntaje = 500
    };

    private static AgregarPistaDto DtoPistaTexto() => new()
    {
        Contenido = "Busca detrás del árbol grande.",
        Tipo = "Texto"
    };

    private static AgregarPistaDto DtoPistaGps() => new()
    {
        Tipo = "CoordenadaGps",
        Latitud = -34.6037,
        Longitud = -58.3816
    };

    private HttpRequestMessage Con(HttpMethod metodo, string url, string rol, object? cuerpo = null)
    {
        var req = new HttpRequestMessage(metodo, url);
        req.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        if (cuerpo is not null)
            req.Content = JsonContent.Create(cuerpo);
        return req;
    }

    private async Task<Guid> CrearBusquedaAsync(string? nombre = null)
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/busquedas", "Administrador", DtoBusqueda(nombre)));
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;
    }

    // ===== AUTH =====

    [Fact]
    public async Task PostBusquedas_SinToken_Retorna401()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/juegos/busquedas", DtoBusqueda());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostBusquedas_ConTokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/busquedas", "Operador", DtoBusqueda()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBusquedasBorrador_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync("/api/juegos/busquedas/borrador");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBusquedasActivas_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync("/api/juegos/busquedas/activas");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===== CRUD BÚSQUEDA =====

    [Fact]
    public async Task PostBusquedas_Administrador_DatosValidos_Retorna201ConId()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/busquedas", "Administrador", DtoBusqueda()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<IdRespuesta>();
        cuerpo!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostBusquedas_NombreDuplicado_Retorna422()
    {
        var nombre = "Busqueda-Unica-" + Guid.NewGuid().ToString("N")[..6];
        await CrearBusquedaAsync(nombre);

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, "/api/juegos/busquedas", "Administrador", DtoBusqueda(nombre)));
        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetBusquedasBorrador_Administrador_Retorna200()
    {
        await CrearBusquedaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/busquedas/borrador", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<BusquedaTesoroResumenDto>>();
        lista.Should().NotBeNull();
        lista!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBusquedasActivas_Operador_Retorna200()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, "/api/juegos/busquedas/activas", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBusquedaDetalle_Existente_Retorna200()
    {
        var id = await CrearBusquedaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/busquedas/{id}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<BusquedaTesoroDetalleDto>();
        detalle!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetBusquedaDetalle_Inexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/busquedas/{Guid.NewGuid()}", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchBusqueda_Administrador_DatosValidos_Retorna204()
    {
        var id = await CrearBusquedaAsync();
        var dto = new ModificarBusquedaTesoroDto
        {
            Nombre = "Busqueda-Modificada-" + Guid.NewGuid().ToString("N")[..6],
            Descripcion = "Nueva descripción del tesoro.",
            Tiempo = 45,
            Puntaje = 800
        };
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteBusqueda_Eliminar_EnBorrador_Retorna204()
    {
        var id = await CrearBusquedaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/busquedas/{id}/eliminar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== ACTIVAR =====

    [Fact]
    public async Task PatchActivarBusqueda_SinPistaGps_Retorna422()
    {
        var id = await CrearBusquedaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/pistas", "Administrador", DtoPistaTexto()));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}/activar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PatchActivarBusqueda_ConPistaGps_Retorna204()
    {
        var id = await CrearBusquedaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/pistas", "Administrador", DtoPistaGps()));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}/activar", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteBusqueda_DesactivarActiva_Retorna204()
    {
        var id = await CrearBusquedaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/pistas", "Administrador", DtoPistaGps()));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}/activar", "Administrador"));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Delete, $"/api/juegos/busquedas/{id}", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== PISTAS =====

    [Fact]
    public async Task PostPista_Administrador_TextoValido_Retorna201()
    {
        var busquedaId = await CrearBusquedaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{busquedaId}/pistas", "Administrador", DtoPistaTexto()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<IdRespuesta>();
        cuerpo!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostPista_Administrador_GpsValido_Retorna201()
    {
        var busquedaId = await CrearBusquedaAsync();
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{busquedaId}/pistas", "Administrador", DtoPistaGps()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostPista_SinToken_Retorna401()
    {
        var respuesta = await _cliente.PostAsJsonAsync($"/api/juegos/busquedas/{Guid.NewGuid()}/pistas", DtoPistaTexto());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutPista_Administrador_Retorna204()
    {
        var busquedaId = await CrearBusquedaAsync();
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{busquedaId}/pistas", "Administrador", DtoPistaTexto()));
        var pistaId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var dto = new ModificarPistaDto { NuevoContenido = "Nueva pista de texto actualizada.", Tipo = "Texto" };
        var respuesta = await _cliente.SendAsync(
            Con(HttpMethod.Put, $"/api/juegos/busquedas/{busquedaId}/pistas/{pistaId}", "Administrador", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePista_Administrador_Retorna204()
    {
        var busquedaId = await CrearBusquedaAsync();
        var crear = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{busquedaId}/pistas", "Administrador", DtoPistaTexto()));
        var pistaId = (await crear.Content.ReadFromJsonAsync<IdRespuesta>())!.Id;

        var respuesta = await _cliente.SendAsync(
            Con(HttpMethod.Delete, $"/api/juegos/busquedas/{busquedaId}/pistas/{pistaId}", "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ===== QR =====

    [Fact]
    public async Task GetCodigoQr_BusquedaActiva_Operador_Retorna200()
    {
        var id = await CrearBusquedaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/pistas", "Administrador", DtoPistaGps()));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}/activar", "Administrador"));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/busquedas/{id}/codigo-qr", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCodigoQr_BusquedaInexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/busquedas/{Guid.NewGuid()}/codigo-qr", "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostValidarCodigoQr_CodigoIncorrecto_RetornaEsValidaFalse()
    {
        var id = await CrearBusquedaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/pistas", "Administrador", DtoPistaGps()));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}/activar", "Administrador"));

        var dto = new ValidarCodigoQrDto { CodigoEscaneado = "CODIGO-INCORRECTO" };
        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/validar-codigo-qr", "Participante", dto));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoQr>();
        resultado!.EsValida.Should().BeFalse();
    }

    // ===== VISTA PARTICIPANTE =====

    [Fact]
    public async Task GetBusquedaParticipante_BusquedaActiva_Retorna200()
    {
        var id = await CrearBusquedaAsync();
        await _cliente.SendAsync(Con(HttpMethod.Post, $"/api/juegos/busquedas/{id}/pistas", "Administrador", DtoPistaGps()));
        await _cliente.SendAsync(Con(HttpMethod.Patch, $"/api/juegos/busquedas/{id}/activar", "Administrador"));

        var respuesta = await _cliente.SendAsync(Con(HttpMethod.Get, $"/api/juegos/busquedas/{id}/participante", "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class IdRespuesta { public Guid Id { get; set; } }
    private sealed class ResultadoQr { public bool EsValida { get; set; } }
}
