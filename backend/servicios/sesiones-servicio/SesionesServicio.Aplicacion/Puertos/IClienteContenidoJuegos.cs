using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

// Puerto saliente hacia juegos-servicio. Devuelve el contenido (Trivia o
// Búsqueda del Tesoro) si existe — independientemente de su estado —
// para que el manejador pueda diferenciar entre "no encontrado" e
// "inactivo" y elegir el código de error correcto. Sólo se considera
// utilizable para crear una sesión cuando EstaActivo == true.
public interface IClienteContenidoJuegos
{
    Task<ContenidoJuegoActivoDto?> ObtenerContenidoAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion);
}
