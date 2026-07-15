using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;

public sealed record ProcesarEquipoCreadoComando(
    Guid EventoId,
    Guid SesionId,
    Guid EquipoId)
    : IRequest;
