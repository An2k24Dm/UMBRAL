using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ModificarOperador;

public sealed record ModificarOperadorComando(
    Guid IdOperador,
    ModificarOperadorSolicitudDto Datos)
    : IRequest<ModificarOperadorRespuestaDto>;
