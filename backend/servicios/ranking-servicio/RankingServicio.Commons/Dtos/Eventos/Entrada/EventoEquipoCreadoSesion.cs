namespace RankingServicio.Commons.Dtos.Eventos.Entrada;

public sealed record EventoEquipoCreadoSesion(
    Guid EventoId,
    Guid SesionId,
    Guid EquipoId);
