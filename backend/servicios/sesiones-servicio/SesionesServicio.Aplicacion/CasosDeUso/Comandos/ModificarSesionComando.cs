using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarSesionComando(Guid Id, ModificarSesionDto Datos)
    : IRequest<SesionDetalleDto>;
