using MediatR;
using SesionesServicio.Commons.Dtos.DesgloseSesion;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;

public sealed record ObtenerMiDesgloseSesionConsulta(Guid SesionId)
    : IRequest<MiDesgloseSesionDto>;
