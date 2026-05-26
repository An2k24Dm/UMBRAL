using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

// HU09 — actualización parcial de un Operador desde el panel del Administrador.
// Sólo se actualizan los campos presentes (no nulos) en el DTO.
public sealed record ModificarOperadorComando(
    Guid IdOperador,
    ModificarOperadorSolicitudDto Datos)
    : IRequest<ModificarOperadorRespuestaDto>;
