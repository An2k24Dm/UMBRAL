using MediatR;
using SesionesServicio.Aplicacion.Comandos.Penalizaciones;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionParticipante;

// HU52 — Aplicar penalización a un participante de una sesión Individual. Solo
// el Operador creador; sesión Activa o Pausada. La cantidad recibida es
// positiva; ranking la interpreta como delta negativo.
public sealed record AplicarPenalizacionParticipanteComando(
    Guid SesionId,
    Guid ParticipanteSesionId,
    int Puntos,
    string? Motivo) : IRequest<PenalizacionEncoladaDto>;
