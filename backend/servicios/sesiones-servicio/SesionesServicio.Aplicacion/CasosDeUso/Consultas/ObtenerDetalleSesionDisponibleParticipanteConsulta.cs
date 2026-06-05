using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerDetalleSesionDisponibleParticipanteConsulta(
    Guid SesionId)
    : IRequest<SesionDetalleMovilDto>;
