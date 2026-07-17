namespace SesionesServicio.Commons.Dtos.ResultadosPuntaje;

public sealed record ResultadoPuntajeDto(
    Guid EventoId,
    bool Procesado,
    int? PuntajeGanado);
