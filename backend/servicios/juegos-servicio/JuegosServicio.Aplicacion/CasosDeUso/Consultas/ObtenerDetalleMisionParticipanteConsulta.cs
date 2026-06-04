using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

// Consulta de detalle de misión para el flujo móvil del Participante.
// El manejador devuelve null si la misión no existe o no está Activa,
// para que el controlador la traduzca a 404. No exponemos borradores
// ni datos administrativos al Participante.
public sealed record ObtenerDetalleMisionParticipanteConsulta(Guid MisionId)
    : IRequest<MisionDetalleParticipanteDto?>;
