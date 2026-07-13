using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Infraestructura.ServiciosExternos;

// Enriquece el nombre de los equipos consultando sesiones-servicio (listado de
// equipos de la sesión), reenviando el token del usuario actual. Si falla o no
// está configurado, devuelve vacío y la consulta usa el id como respaldo.
public sealed class ClienteSesionesRankingHttp : IClienteSesionesRanking
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;
    private readonly ILogger<ClienteSesionesRankingHttp> _registro;
    private readonly bool _urlConfigurada;

    public ClienteSesionesRankingHttp(
        HttpClient cliente,
        IOptions<OpcionesSesionesServicio> opciones,
        IPropagadorTokenActual propagador,
        ILogger<ClienteSesionesRankingHttp> registro)
    {
        _cliente = cliente;
        _propagador = propagador;
        _registro = registro;

        var url = opciones.Value.Url?.Trim();
        _urlConfigurada = !string.IsNullOrWhiteSpace(url);
        if (_urlConfigurada)
            _cliente.BaseAddress = new Uri(url!.TrimEnd('/') + "/");
        else
            _registro.LogError(
                "ServiciosExternos:SesionesServicio:Url no está configurada. " +
                "No se podrán resolver los nombres de los equipos.");
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresEquiposAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var vacio = new Dictionary<Guid, string>();
        if (sesionId == Guid.Empty || !_urlConfigurada)
            return vacio;

        try
        {
            using var solicitud = new HttpRequestMessage(
                HttpMethod.Get, $"api/sesiones/{sesionId}/equipos");

            var token = _propagador.ObtenerTokenActual();
            if (!string.IsNullOrWhiteSpace(token))
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);
            if (!respuesta.IsSuccessStatusCode)
            {
                _registro.LogWarning(
                    "sesiones-servicio respondió {Estado} al resolver equipos de la sesión {SesionId}.",
                    (int)respuesta.StatusCode, sesionId);
                return vacio;
            }

            var cuerpo = await respuesta.Content
                .ReadFromJsonAsync<List<EquipoResumenDto>>(OpcionesJson, cancelacion);

            if (cuerpo is null) return vacio;
            return cuerpo
                .Where(e => e.Id != Guid.Empty)
                .GroupBy(e => e.Id)
                .ToDictionary(g => g.Key, g => g.First().Nombre ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Error al consultar sesiones-servicio para resolver equipos de la sesión {SesionId}.",
                sesionId);
            return vacio;
        }
    }

    private sealed class EquipoResumenDto
    {
        public Guid Id { get; set; }
        public string? Nombre { get; set; }
    }
}
