using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarEvidenciaTesoro;

public sealed record ProcesarEvidenciaTesoroComando(
    Guid EventoId,
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    Guid BusquedaId,
    bool EsValida,
    int PuntajeBase)
    : IRequest;
