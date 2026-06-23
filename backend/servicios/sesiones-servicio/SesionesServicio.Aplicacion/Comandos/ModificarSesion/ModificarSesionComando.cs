using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.ModificarSesion;

public sealed record ModificarSesionComando(Guid Id, ModificarSesionDto Datos)
    : IRequest<SesionDetalleDto>;
