using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteEquipo;

// HU45 — Los tres ids viajan en la ruta; el actor (líder u Operador) se
// resuelve del usuario autenticado, no del body. No devuelve cuerpo (204).
public sealed record ExpulsarParticipanteEquipoComando(
    Guid SesionId,
    Guid EquipoId,
    Guid ParticipanteSesionId) : IRequest;
