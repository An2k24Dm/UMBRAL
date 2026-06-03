using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

// Adaptador HTTP hacia juegos-servicio para consultar misiones. Reenvía
// el token Bearer del usuario actual para que la autorización por roles
// del microservicio destino se aplique de forma transparente.
//
// Endpoint contractual: GET /api/juegos/misiones/{misionId}
public sealed class ClienteJuegosMisionesHttp : IClienteJuegosMisiones
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;

    public ClienteJuegosMisionesHttp(
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

    public async Task<MisionResumenJuegosDto?> ObtenerMisionAsync(
        Guid misionId, CancellationToken cancelacion)
    {
        var bruto = await EnviarAsync<MisionDetalleRespuesta>(
            $"api/juegos/misiones/{misionId}", cancelacion);
        if (bruto is null) return null;

        return new MisionResumenJuegosDto
        {
            Id = bruto.Id,
            Nombre = bruto.Nombre ?? string.Empty,
            Descripcion = bruto.Descripcion ?? string.Empty,
            Estado = bruto.Estado ?? string.Empty,
            Dificultad = bruto.Dificultad ?? string.Empty,
            TotalEtapas = bruto.Etapas?.Count ?? 0
        };
    }

    private async Task<T?> EnviarAsync<T>(string ruta, CancellationToken cancelacion)
        where T : class
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;

        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson, cancelacion);
    }

    // Espejo liviano del MisionDetalleDto de juegos-servicio. Sólo lee
    // los campos que sesiones-servicio necesita.
    private sealed class MisionDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Estado { get; set; }
        public string? Dificultad { get; set; }
        public List<EtapaRespuesta>? Etapas { get; set; }
    }

    private sealed class EtapaRespuesta
    {
        public Guid Id { get; set; }
    }
}
