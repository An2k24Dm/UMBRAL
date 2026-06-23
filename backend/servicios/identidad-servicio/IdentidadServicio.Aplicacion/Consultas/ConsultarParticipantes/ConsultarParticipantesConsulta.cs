using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Consultas.ConsultarParticipantes;

// HU07: listado paginado de Participantes para el panel web.
// - Pagina y TamanioPagina llevan valores por defecto (1 y 10).
// - OrdenEstado puede ser "asc", "desc" o nulo/vacío.
public sealed record ConsultarParticipantesConsulta(
    int Pagina,
    int TamanioPagina,
    string? OrdenEstado) : IRequest<ResultadoPaginadoDto<ParticipanteListadoDto>>;
