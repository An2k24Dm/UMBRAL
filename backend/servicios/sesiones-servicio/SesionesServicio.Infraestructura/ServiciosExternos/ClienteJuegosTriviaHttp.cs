using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Infraestructura.Configuraciones;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class ClienteJuegosTriviaHttp : IClienteJuegosTrivia
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;

    public ClienteJuegosTriviaHttp(
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

    public Task<TriviaParticipanteJuegosDto?> ObtenerTriviaParticipanteAsync(
        Guid triviaId, CancellationToken cancelacion)
        => GetAsync<TriviaParticipanteJuegosDto>(
            $"api/juegos/trivias/{triviaId}/participante", cancelacion);

    public async Task<VerificacionRespuestaJuegosDto?> VerificarRespuestaAsync(
        Guid triviaId, Guid preguntaId, Guid opcionSeleccionadaId, CancellationToken cancelacion)
    {
        var cuerpo = JsonSerializer.Serialize(
            new { opcionSeleccionadaId },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/juegos/trivias/{triviaId}/preguntas/{preguntaId}/verificar")
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
        };

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            return null;

        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<VerificacionRespuestaJuegosDto>(
            OpcionesJson, cancelacion);
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
}
