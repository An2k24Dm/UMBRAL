using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record CrearSesionComando(CrearSesionSolicitudDto Datos)
    : IRequest<CrearSesionRespuestaDto>;
