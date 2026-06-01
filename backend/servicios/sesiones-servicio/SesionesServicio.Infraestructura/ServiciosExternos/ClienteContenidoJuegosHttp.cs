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
// Endpoints contractuales esperados en juegos-servicio:
//   GET /api/juegos/contenidos-activos/{tipoJuego}/{id}
//   GET /api/juegos/trivias/{triviaId}
//   GET /api/juegos/busquedas/{busquedaId}
//
// Si juegos-servicio responde 404, los métodos devuelven null. El
// manejador decide si esto es "no encontrado al crear" o "contenido
// no disponible al consultar detalle".
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

    public Task<ContenidoJuegoActivoDto?> ObtenerContenidoAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion)
        => EnviarAsync<ContenidoJuegoActivoDto>(
            $"api/juegos/contenidos-activos/{tipoJuego}/{contenidoJuegoId}",
            cancelacion);

    public async Task<DetalleTriviaSesionDto?> ObtenerDetalleTriviaAsync(
        Guid triviaId, CancellationToken cancelacion)
    {
        var bruto = await EnviarAsync<TriviaDetalleRespuesta>(
            $"api/juegos/trivias/{triviaId}", cancelacion);
        if (bruto is null) return null;

        return new DetalleTriviaSesionDto
        {
            Id = bruto.Id,
            Nombre = bruto.Nombre ?? string.Empty,
            Descripcion = bruto.Descripcion ?? string.Empty,
            Estado = bruto.Estado ?? string.Empty,
            Preguntas = bruto.Preguntas?.Select(p => new PreguntaTriviaSesionDto
            {
                Id = p.Id,
                Enunciado = p.Enunciado ?? string.Empty,
                PuntajeAsignado = p.PuntajeAsignado,
                Opciones = p.Opciones?.Select(o => new OpcionTriviaSesionDto
                {
                    Id = o.Id,
                    Texto = o.Texto ?? string.Empty,
                    EsCorrecta = o.EsCorrecta
                }).ToList() ?? new List<OpcionTriviaSesionDto>()
            }).ToList() ?? new List<PreguntaTriviaSesionDto>()
        };
    }

    public async Task<DetalleBusquedaSesionDto?> ObtenerDetalleBusquedaTesoroAsync(
        Guid busquedaId, CancellationToken cancelacion)
    {
        var bruto = await EnviarAsync<BusquedaDetalleRespuesta>(
            $"api/juegos/busquedas/{busquedaId}", cancelacion);
        if (bruto is null) return null;

        return new DetalleBusquedaSesionDto
        {
            Id = bruto.Id,
            Nombre = bruto.Nombre ?? string.Empty,
            Descripcion = bruto.Descripcion ?? string.Empty,
            Estado = bruto.Estado ?? string.Empty,
            Etapas = bruto.Etapas?
                .OrderBy(e => e.Orden)
                .Select(e => new EtapaBusquedaSesionDto
                {
                    Id = e.Id,
                    // juegos-servicio expone el campo como "Titulo".
                    Nombre = e.Titulo ?? string.Empty,
                    Descripcion = e.Descripcion ?? string.Empty,
                    Orden = e.Orden,
                    Pistas = e.Pistas?.Select((p, indice) => new PistaBusquedaSesionDto
                    {
                        Id = p.Id,
                        // juegos-servicio expone el campo como "Contenido".
                        Texto = p.Contenido ?? string.Empty,
                        Orden = indice + 1
                    }).ToList() ?? new List<PistaBusquedaSesionDto>()
                }).ToList() ?? new List<EtapaBusquedaSesionDto>()
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

    // Espejo liviano de los DTOs de juegos-servicio para no acoplar
    // este proyecto a JuegosServicio.Commons. Solo necesitamos las
    // propiedades que mostramos en el detalle de la sesión.
    private sealed class TriviaDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Estado { get; set; }
        public List<PreguntaDetalleRespuesta>? Preguntas { get; set; }
    }

    private sealed class PreguntaDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Enunciado { get; set; }
        public int PuntajeAsignado { get; set; }
        public List<OpcionDetalleRespuesta>? Opciones { get; set; }
    }

    private sealed class OpcionDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Texto { get; set; }
        public bool EsCorrecta { get; set; }
    }

    private sealed class BusquedaDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Estado { get; set; }
        public List<EtapaDetalleRespuesta>? Etapas { get; set; }
    }

    private sealed class EtapaDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public List<PistaDetalleRespuesta>? Pistas { get; set; }
    }

    private sealed class PistaDetalleRespuesta
    {
        public Guid Id { get; set; }
        public string? Contenido { get; set; }
    }
}
