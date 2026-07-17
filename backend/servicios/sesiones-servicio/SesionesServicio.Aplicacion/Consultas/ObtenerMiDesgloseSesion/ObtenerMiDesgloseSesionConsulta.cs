using MediatR;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;

public sealed record ObtenerMiDesgloseSesionConsulta(Guid SesionId)
    : IRequest<MiDesgloseSesionDto>;

public sealed record MiDesgloseSesionDto(
    Guid ParticipanteIdentidadId,
    long PuntajeBruto,
    long PuntosPenalizados,
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
