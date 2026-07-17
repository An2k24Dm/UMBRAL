using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Commons.Dtos.ServiciosExternos;

namespace RankingServicio.Infraestructura.ServiciosExternos;

public sealed class ClienteIdentidadParticipantesRanking : IClienteIdentidadParticipantes
{
    private const string Ruta = "api/usuarios/participantes/por-ids";

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;
    private readonly ILogger<ClienteIdentidadParticipantesRanking> _registro;
    private readonly bool _urlConfigurada;

    public ClienteIdentidadParticipantesRanking(
        HttpClient cliente,
        IOptions<OpcionesIdentidadServicio> opciones,
        IPropagadorTokenActual propagador,
        ILogger<ClienteIdentidadParticipantesRanking> registro)
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
                "ServiciosExternos:IdentidadServicio:Url no está configurada. " +
                "No se podrán resolver los alias de los participantes.");
    }

    public async Task<IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>> ObtenerParticipantesPorIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancelacion)
    {
        var lista = ids?.Where(i => i != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        var vacio = new Dictionary<Guid, ParticipanteIdentidadResumenDto>();
        if (lista.Count == 0 || !_urlConfigurada)
            return vacio;

        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, Ruta)
            {
                Content = JsonContent.Create(
                    new { participantesIds = lista }, options: OpcionesJson)
            };

            var token = _propagador.ObtenerTokenActual();
            if (!string.IsNullOrWhiteSpace(token))
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);
            if (!respuesta.IsSuccessStatusCode)
            {
                _registro.LogWarning(
                    "identidad-servicio respondió {Estado} al resolver participantes.",
                    (int)respuesta.StatusCode);
                return vacio;
            }

            var cuerpo = await respuesta.Content
                .ReadFromJsonAsync<List<ParticipanteIdentidadResumenDto>>(OpcionesJson, cancelacion);

            if (cuerpo is null) return vacio;
            return cuerpo
                .Where(p => p.Id != Guid.Empty)
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Error al consultar identidad-servicio para resolver participantes.");
            return vacio;
        }
    }
}
