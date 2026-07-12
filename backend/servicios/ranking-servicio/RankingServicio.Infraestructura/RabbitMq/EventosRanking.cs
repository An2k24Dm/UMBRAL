namespace RankingServicio.Infraestructura.RabbitMq;

public sealed record EventoRespuestaTriviaRegistrada(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteIdentidadId,
    string NombreParticipante,
    Guid? EquipoId,
    string? NombreEquipo,
    int Puntaje,
    bool EsCorrecta);

public sealed record EventoEvidenciaTesoroRegistrada(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteIdentidadId,
    string NombreParticipante,
    Guid? EquipoId,
    string? NombreEquipo,
    int Puntaje);

public sealed record EventoSesionFinalizada(
    Guid EventoId,
    Guid SesionId,
    bool EsGrupal);

public sealed record EventoParticipanteUnidoSesion(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteIdentidadId,
    string NombreParticipante);

public sealed record EventoEquipoCreadoSesion(
    Guid EventoId,
    Guid SesionId,
    Guid EquipoId,
    string NombreEquipo);
