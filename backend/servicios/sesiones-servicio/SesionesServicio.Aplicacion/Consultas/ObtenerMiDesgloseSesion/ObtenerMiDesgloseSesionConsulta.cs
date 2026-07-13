using MediatR;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;

// Desglose del puntaje del participante autenticado por misión y etapa. El
// puntaje sale de sesiones-servicio (RespuestaTrivia/EvidenciaTesoro.PuntosGanados,
// fijado con el valor real que calcula ranking); los nombres/orden de
// misiones/etapas se enriquecen desde juegos-servicio.
public sealed record ObtenerMiDesgloseSesionConsulta(Guid SesionId)
    : IRequest<MiDesgloseSesionDto>;

public sealed record MiDesgloseSesionDto(
    Guid ParticipanteIdentidadId,
    long PuntajeTotal,
    IReadOnlyList<DesgloseMisionDto> Misiones);

public sealed record DesgloseMisionDto(
    Guid MisionId,
    int Orden,
    string Nombre,
    long PuntajeTotal,
    IReadOnlyList<DesgloseEtapaDto> Etapas);

public sealed record DesgloseEtapaDto(
    Guid EtapaId,
    int Orden,
    string Nombre,
    string Tipo,
    long Puntaje);
