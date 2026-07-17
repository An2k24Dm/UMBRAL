using MediatR;
using SesionesServicio.Commons.Dtos.Penalizaciones;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionEquipo;

public sealed record AplicarPenalizacionEquipoComando(
    Guid SesionId,
    Guid EquipoId,
    int Puntos,
    string? Motivo) : IRequest<PenalizacionEncoladaDto>;
