using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

// HU34 — Adaptador HTTP del puerto IClienteIdentidadUsuarios.
//
// Reenvía el token Bearer del usuario actual para que la política
// PoliticaAdministradorUOperador del endpoint destino se aplique de
// forma transparente.
//
// Contrato del endpoint en identidad-servicio:
//   POST /api/usuarios/internos/administradores-por-ids
//     body: { usuariosIds: [...] }
//     response: { administradoresIds: [...] }
//
// Política de fallos: cualquier problema (URL no configurada, identidad
// caído, 4xx/5xx, JSON malformado) se reporta en el log y se traduce a
// "ninguno es administrador". De esa forma:
//   * Para Administrador: el listado funciona porque no llama a este
//     puerto (lo decide ListarSesionesManejador).
//   * Para Operador: en vez de explotar con 500, sólo ve sus propias
//     sesiones — modo seguro. El log explica qué falló.
public sealed class ClienteIdentidadUsuariosHttp : IClienteIdentidadUsuarios
{
    private const string Ruta = "api/usuarios/internos/administradores-por-ids";

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;
    private readonly ILogger<ClienteIdentidadUsuariosHttp> _registro;
    private readonly bool _urlConfigurada;

    public ClienteIdentidadUsuariosHttp(
        HttpClient cliente,
        IOptions<OpcionesIdentidadServicio> opciones,
        IPropagadorTokenActual propagador,
        ILogger<ClienteIdentidadUsuariosHttp> registro)
    {
        _cliente = cliente;
        _propagador = propagador;
        _registro = registro;

        var url = opciones.Value.Url?.Trim();
        _urlConfigurada = !string.IsNullOrWhiteSpace(url);
        if (_urlConfigurada)
        {
            _cliente.BaseAddress = new Uri(url!.TrimEnd('/') + "/");
        }
        else
        {
            _registro.LogError(
                "ServiciosExternos:IdentidadServicio:Url no está configurada. " +
                "El listado del Operador no podrá identificar sesiones creadas por Administrador.");
        }
    }

    public async Task<bool> EsAdministradorAsync(
        Guid usuarioId, CancellationToken cancelacion)
    {
        var administradores = await FiltrarAdministradoresAsync(
            new[] { usuarioId }, cancelacion);
        return administradores.Contains(usuarioId);
    }

    public async Task<IReadOnlyCollection<Guid>> FiltrarAdministradoresAsync(
        IReadOnlyCollection<Guid> usuariosIds, CancellationToken cancelacion)
    {
        if (usuariosIds is null || usuariosIds.Count == 0)
            return Array.Empty<Guid>();

        if (!_urlConfigurada)
            return Array.Empty<Guid>();

        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, Ruta)
            {
                Content = JsonContent.Create(
                    new { usuariosIds }, options: OpcionesJson)
            };

            var token = _propagador.ObtenerTokenActual();
            if (!string.IsNullOrWhiteSpace(token))
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                _registro.LogWarning(
                    "identidad-servicio respondió {Estado} al filtrar administradores. " +
                    "Se ignora la respuesta y se asume ningún administrador.",
                    (int)respuesta.StatusCode);
                return Array.Empty<Guid>();
            }

            var cuerpo = await respuesta.Content
                .ReadFromJsonAsync<RespuestaAdministradoresPorIds>(OpcionesJson, cancelacion);

            return cuerpo?.AdministradoresIds ?? Array.Empty<Guid>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Cualquier fallo de red, DNS, timeout, deserialización, etc.
            // Se registra pero NO se propaga: el manejador del listado
            // queda en modo seguro (sólo propias del Operador).
            _registro.LogError(ex,
                "Error al consultar identidad-servicio para filtrar administradores. " +
                "Se asume ningún administrador en este ciclo.");
            return Array.Empty<Guid>();
        }
    }

    private sealed class RespuestaAdministradoresPorIds
    {
        public IReadOnlyCollection<Guid> AdministradoresIds { get; set; } = Array.Empty<Guid>();
    }
}
