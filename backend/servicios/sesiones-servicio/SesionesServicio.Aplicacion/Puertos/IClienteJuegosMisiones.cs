using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Puertos;

// Puerto saliente hacia juegos-servicio para resolver Misiones.
//
// Reemplaza al antiguo IClienteContenidoJuegos (que apuntaba a Trivia y
// Búsqueda directamente): ahora una Sesion se asocia a una o varias
// Misiones, y juegos-servicio es la fuente de verdad para conocer su
// estado (Activa/Inactiva) y su cantidad de etapas.
public interface IClienteJuegosMisiones
{
    Task<MisionResumenJuegosDto?> ObtenerMisionAsync(
        Guid misionId, CancellationToken cancelacion);

    // Devuelve la misión con sus etapas reales. Lo consume el detalle
    // móvil de sesión, que necesita orden + tipo + nombre + tiempo
    // estimado de cada etapa para mostrar al Participante.
    Task<MisionConEtapasJuegosDto?> ObtenerMisionConEtapasAsync(
        Guid misionId, CancellationToken cancelacion);
}
