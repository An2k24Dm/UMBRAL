using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.LiberarPista;

public sealed record LiberarPistaComando(
    Guid SesionId,
    Guid EtapaId,
    Guid? PistaId,
    string? Contenido,
    string Tipo,
    double? Latitud,
    double? Longitud) : IRequest;
