using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class ClienteBusquedaTesoroHttp : IClienteBusquedaTesoro
{
    private static readonly System.Text.Json.JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;

    public ClienteBusquedaTesoroHttp(
        HttpClient cliente,
        IOptions<OpcionesJuegosServicio> opciones,
        IPropagadorTokenActual propagador)
    {
        _cliente = cliente;
        _propagador = propagador;

        if (!string.IsNullOrWhiteSpace(opciones.Value.Url))
        {
            var url = opciones.Value.Url.TrimEnd('/') + "/";
            _cliente.BaseAddress = new Uri(url);
        }
    }

    public Task<BusquedaTesoroJuegosDto?> ObtenerBusquedaParticipanteAsync(
        Guid busquedaId, CancellationToken cancelacion)
        => GetAsync<BusquedaTesoroJuegosDto>(
            $"api/juegos/busquedas/{busquedaId}/participante", cancelacion);

    public async Task<bool?> ValidarCodigoQrAsync(
        Guid busquedaId, string codigoEscaneado, CancellationToken cancelacion)
    {
        var resultado = await PostAsync<ValidarSolicitud, ValidarRespuesta>(
            $"api/juegos/busquedas/{busquedaId}/validar-codigo-qr",
            new ValidarSolicitud(codigoEscaneado),
            cancelacion);
        return resultado?.EsValida;
    }

    private async Task<T?> GetAsync<T>(string ruta, CancellationToken cancelacion)
        where T : class
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            return null;

        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson, cancelacion);
    }

    private async Task<TRespuesta?> PostAsync<TSolicitud, TRespuesta>(
        string ruta, TSolicitud cuerpo, CancellationToken cancelacion)
        where TSolicitud : class
        where TRespuesta : class
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = JsonContent.Create(cuerpo)
        };

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            return null;

        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<TRespuesta>(OpcionesJson, cancelacion);
    }

    private sealed record ValidarSolicitud(string CodigoEscaneado);
    private sealed record ValidarRespuesta(bool EsValida);
}
