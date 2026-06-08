using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Consultas;

// HU07: detalle/perfil completo de un Participante seleccionado.
// Si el id corresponde a un Operador o Administrador el manejador debe
// devolver "no encontrado" — este caso de uso solo aplica a Participantes.
public sealed record ObtenerParticipanteDetalleConsulta(Guid Id)
    : IRequest<PerfilParticipanteDto>;
