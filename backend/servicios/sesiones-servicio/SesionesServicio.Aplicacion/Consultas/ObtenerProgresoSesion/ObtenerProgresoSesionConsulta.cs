using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;

public sealed record ObtenerProgresoSesionConsulta(Guid SesionId)
    : IRequest<IReadOnlyList<ProgresoSesionParticipanteDto>>;
