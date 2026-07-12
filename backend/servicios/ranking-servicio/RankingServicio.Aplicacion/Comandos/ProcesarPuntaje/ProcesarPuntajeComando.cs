using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPuntaje;

public sealed record ProcesarPuntajeComando(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteIdentidadId,
    string NombreParticipante,
    Guid? EquipoId,
    string? NombreEquipo,
    int Puntaje,
    bool EsCorrecta,
    string TipoJuego)
    : IRequest;
