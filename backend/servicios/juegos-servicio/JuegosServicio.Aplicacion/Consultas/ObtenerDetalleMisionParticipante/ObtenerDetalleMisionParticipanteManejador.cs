using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerDetalleMisionParticipante;

// Reutiliza ObtenerDetalleMisionAsync (el repositorio ya devuelve el
// detalle completo con etapas) y lo proyecta a un DTO más chico
// específico para el Participante.
//
// Reglas:
//   * Si la misión no existe → null → 404 en el controlador.
//   * Si la misión no está "Activa" → null → 404 en el controlador.
//     El Participante no debe poder consultar borradores, aunque alguna
//     sesión vieja siga referenciándolos.
//   * Nunca emite datos administrativos (creadorId, fecha de creación).
public sealed class ObtenerDetalleMisionParticipanteManejador
    : IRequestHandler<ObtenerDetalleMisionParticipanteConsulta,
        MisionDetalleParticipanteDto?>
{
    private const string EstadoActiva = "Activa";

    private readonly IRepositorioMisiones _repositorio;

    public ObtenerDetalleMisionParticipanteManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<MisionDetalleParticipanteDto?> Handle(
        ObtenerDetalleMisionParticipanteConsulta consulta,
        CancellationToken cancelacion)
    {
        var detalle = await _repositorio.ObtenerDetalleMisionAsync(
            consulta.MisionId, cancelacion);

        if (detalle is null) return null;

        // Defensa en profundidad: aunque la creación de sesión solo
        // permita asociar misiones Activas, el Participante no debe ver
        // borradores aún si la misión cambió de estado después.
        if (!string.Equals(detalle.Estado, EstadoActiva, StringComparison.OrdinalIgnoreCase))
            return null;

        return new MisionDetalleParticipanteDto
        {
            Id = detalle.Id,
            Nombre = detalle.Nombre,
            Descripcion = detalle.Descripcion,
            Estado = detalle.Estado,
            Dificultad = detalle.Dificultad,
            Etapas = detalle.Etapas
                .OrderBy(e => e.Orden)
                .Select(e => new EtapaMisionParticipanteDto
                {
                    Id = e.Id,
                    Orden = e.Orden,
                    TipoModoDeJuego = e.TipoModoDeJuego,
                    ModoDeJuegoId = e.ModoDeJuegoId,
                    NombreModoDeJuego = e.NombreModoDeJuego,
                    TiempoEstimado = e.TiempoEstimado
                }).ToList()
        };
    }
}
