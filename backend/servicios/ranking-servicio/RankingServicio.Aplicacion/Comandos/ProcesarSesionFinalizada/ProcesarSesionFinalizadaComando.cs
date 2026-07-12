using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarSesionFinalizada;

public sealed record ProcesarSesionFinalizadaComando(
    Guid EventoId,
    Guid SesionId,
    bool EsGrupal)
    : IRequest;
