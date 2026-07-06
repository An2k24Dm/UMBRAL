using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Infraestructura.ServiciosExternos;

public sealed class ClienteJuegosHttp : IClienteJuegos
{
    private readonly HttpClient _http;
    private readonly IPropagadorTokenActual _token;

    public ClienteJuegosHttp(HttpClient http, IPropagadorTokenActual token, IOptions<OpcionesJuegosServicio> opciones)
    {
        _http = http;
        _token = token;
        _http.BaseAddress = new Uri(opciones.Value.UrlBase);
    }

    public async Task<VerificacionRespuestaDto?> VerificarRespuestaAsync(
        Guid triviaId, Guid preguntaId, Guid opcionId, CancellationToken cancelacion)
    {
        var tokenValor = _token.ObtenerTokenActual();
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/juegos/trivias/{triviaId}/preguntas/{preguntaId}/verificar?opcionId={opcionId}");

        if (!string.IsNullOrWhiteSpace(tokenValor))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValor);

        var respuesta = await _http.SendAsync(solicitud, cancelacion);
        if (!respuesta.IsSuccessStatusCode) return null;

        return await respuesta.Content.ReadFromJsonAsync<VerificacionRespuestaDto>(cancelacion);
    }
}
