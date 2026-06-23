using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerDetalleSesionDisponibleParticipante;

public sealed record ObtenerDetalleSesionDisponibleParticipanteConsulta(
    Guid SesionId)
    : IRequest<SesionDetalleMovilDto>;
