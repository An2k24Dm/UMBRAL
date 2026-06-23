using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.RegistrarParticipante;

// HU03 — comando del caso de uso de registro público de Participante desde la
// app móvil. Reutiliza CrearUsuarioRespuestaDto para mantener consistente la
// forma de respuesta de creación de usuarios.
public sealed record RegistrarParticipanteComando(RegistrarParticipanteDto Datos)
    : IRequest<CrearUsuarioRespuestaDto>;
