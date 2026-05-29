using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

// Adaptador HTTP hacia juegos-servicio. Reenvía el token Bearer del
// usuario actual para que la autorización por roles del microservicio
// destino se aplique de forma transparente.
//
// El endpoint contrato esperado en juegos-servicio es:
//   GET /api/juegos/contenidos-activos/{tipoJuego}/{id}
// y devuelve un ContenidoJuegoActivoDto. Si el contenido no existe,
// responde 404 y el adaptador devuelve null.
public sealed class ClienteContenidoJuegosHttp : IClienteContenidoJuegos
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _cliente;
    private readonly IPropagadorTokenActual _propagador;

    public ClienteContenidoJuegosHttp(
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

    public async Task<ContenidoJuegoActivoDto?> ObtenerContenidoAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion)
    {
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/juegos/contenidos-activos/{tipoJuego}/{contenidoJuegoId}");

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;

        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<ContenidoJuegoActivoDto>(
            OpcionesJson, cancelacion);
    }
}
