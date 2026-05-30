using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using Microsoft.Extensions.Options;

namespace JuegosServicio.Infraestructura.ServiciosExternos;

// Adaptador HTTP hacia sesiones-servicio. Llama al endpoint:
//   GET /api/sesiones/contenidos/{tipoJuego}/{contenidoJuegoId}/existe-vigente
// y devuelve true si existe al menos una sesión vigente asociada al
// contenido. Reenvía el token Bearer del usuario actual para que la
// autorización por roles del microservicio destino se aplique de
// forma transparente.
//
// Política de errores:
//   * 200 → se interpreta el cuerpo { "existe": bool }.
//   * 404 → se trata como "no existen sesiones para ese contenido"
//     (la base de sesiones está vacía o el endpoint reportó nada
//     coincidente). En la práctica el endpoint responde 200 con
//     existe=false, pero dejamos el 404 cubierto por defensa en
//     profundidad.
//   * Cualquier otro código se propaga como HttpRequestException para
//     no enmascarar fallos de red o de configuración (el manejador
//     traduce eso a 500 vía middleware).
public sealed class ClienteSesionesHttp : IClienteSesiones
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        {
            var url = opciones.Value.Url.TrimEnd('/') + "/";
            _cliente.BaseAddress = new Uri(url);
        }
    }

    public async Task<bool> ExisteSesionVigentePorContenidoAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion)
    {
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/sesiones/contenidos/{tipoJuego}/{contenidoJuegoId}/existe-vigente");

        var token = _propagador.ObtenerTokenActual();
        if (!string.IsNullOrWhiteSpace(token))
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);

        if (respuesta.StatusCode == HttpStatusCode.NotFound) return false;

        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content
            .ReadFromJsonAsync<RespuestaExisteVigente>(OpcionesJson, cancelacion);

        return cuerpo?.Existe ?? false;
    }

    // DTO interno (no compartido con Commons) para no acoplar el
    // contrato del cliente al del DTO de sesiones-servicio. Si en el
    // futuro sesiones agrega más campos a la respuesta, este record
    // sigue funcionando porque ignoramos lo que no entendemos.
    private sealed record RespuestaExisteVigente(bool Existe);
}
