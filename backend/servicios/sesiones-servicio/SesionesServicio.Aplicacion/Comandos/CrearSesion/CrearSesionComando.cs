using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.CrearSesion;

public sealed record CrearSesionComando(CrearSesionSolicitudDto Datos)
    : IRequest<CrearSesionRespuestaDto>;
