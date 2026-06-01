using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

// Puerto saliente hacia juegos-servicio.
//
// ObtenerContenidoAsync: devuelve el resumen (con su estado y bandera
// EstaActivo) para que el manejador pueda diferenciar entre "no
// encontrado" e "inactivo" al crear una sesión.
//
// HU34/5.2 — Ampliamos el puerto con dos lecturas de detalle. Las usan
// los manejadores de Listar/Detalle para enriquecer la respuesta con
// la información del contenido sin guardarla en la base de sesiones.
public interface IClienteContenidoJuegos
{
    Task<ContenidoJuegoActivoDto?> ObtenerContenidoAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion);

    Task<DetalleTriviaSesionDto?> ObtenerDetalleTriviaAsync(
        Guid triviaId, CancellationToken cancelacion);

    Task<DetalleBusquedaSesionDto?> ObtenerDetalleBusquedaTesoroAsync(
        Guid busquedaId, CancellationToken cancelacion);
}
