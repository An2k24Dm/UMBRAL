using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Infraestructura.ServiciosExternos;

public sealed class ClienteSesionesHttp : IClienteSesiones
{
    private readonly HttpClient _http;
    private readonly IPropagadorTokenActual _token;

    public ClienteSesionesHttp(HttpClient http, IPropagadorTokenActual token, IOptions<OpcionesSesionesServicio> opciones)
    {
        _http = http;
        _token = token;
        _http.BaseAddress = new Uri(opciones.Value.UrlBase);
    }

    public async Task<InfoPartidaSesionDto?> ObtenerInfoPartidaAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var tokenValor = _token.ObtenerTokenActual();
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get, $"api/sesiones/{sesionId}/estado-partida");

        if (!string.IsNullOrWhiteSpace(tokenValor))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValor);

        var respuesta = await _http.SendAsync(solicitud, cancelacion);
        if (!respuesta.IsSuccessStatusCode) return null;

        return await respuesta.Content.ReadFromJsonAsync<InfoPartidaSesionDto>(cancelacion);
    }

    public async Task<NombresRankingClienteDto?> ObtenerNombresRankingAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var tokenValor = _token.ObtenerTokenActual();
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get, $"api/sesiones/{sesionId}/nombres-ranking");

        if (!string.IsNullOrWhiteSpace(tokenValor))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValor);

        var respuesta = await _http.SendAsync(solicitud, cancelacion);
        if (!respuesta.IsSuccessStatusCode) return null;

        return await respuesta.Content.ReadFromJsonAsync<NombresRankingClienteDto>(cancelacion);
    }
}
