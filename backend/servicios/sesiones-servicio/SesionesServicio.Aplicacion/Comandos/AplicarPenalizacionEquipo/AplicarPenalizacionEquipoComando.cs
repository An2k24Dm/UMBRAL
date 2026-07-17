using MediatR;
using SesionesServicio.Aplicacion.Comandos.Penalizaciones;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionEquipo;

public sealed record AplicarPenalizacionEquipoComando(
    Guid SesionId,
    Guid EquipoId,
    int Puntos,
    string? Motivo) : IRequest<PenalizacionEncoladaDto>;
