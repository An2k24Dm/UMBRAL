using System.Net.Http.Headers;
using System.Net.Http.Json;
using JuegosServicio.Aplicacion.Puertos;
using Microsoft.Extensions.Options;

namespace JuegosServicio.Infraestructura.ServiciosExternos;

public sealed class ClienteSesionesHttp : IClienteSesiones
{
    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;

    public ClienteSesionesHttp(
        HttpClient cliente,
        IOptions<OpcionesSesionesServicio> opciones,
        IPropagadorTokenActual propagador)
    {
        _cliente = cliente;
        _propagador = propagador;

        if (!string.IsNullOrWhiteSpace(opciones.Value.Url))
            _cliente.BaseAddress = new Uri(opciones.Value.Url.TrimEnd('/') + "/");
    }

    public async Task<bool> ExisteSesionVigentePorMisionAsync(
        Guid misionId, CancellationToken cancelacion)
    {
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/sesiones/misiones/{misionId}/existe-vigente");

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (!respuesta.IsSuccessStatusCode) return false;

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ExisteRespuesta>(
            cancellationToken: cancelacion);

        return cuerpo?.Existe ?? false;
    }

    private sealed class ExisteRespuesta
    {
        public bool Existe { get; init; }
    }
}
