using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSecuencialSesion;

public sealed record ObtenerProgresoSecuencialSesionConsulta(Guid SesionId)
    : IRequest<ProgresoSecuencialSesionDto>;
