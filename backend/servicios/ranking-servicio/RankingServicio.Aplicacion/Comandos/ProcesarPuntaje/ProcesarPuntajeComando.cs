using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPuntaje;

public sealed record ProcesarPuntajeComando(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    int Puntaje,
    string TipoJuego)
    : IRequest;
