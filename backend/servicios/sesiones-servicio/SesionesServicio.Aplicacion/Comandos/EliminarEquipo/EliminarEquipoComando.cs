using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.EliminarEquipo;

public sealed record EliminarEquipoComando(
    Guid SesionId,
    Guid EquipoId) : IRequest;
