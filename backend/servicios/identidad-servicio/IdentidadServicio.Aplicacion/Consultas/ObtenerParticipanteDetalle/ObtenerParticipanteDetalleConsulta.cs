using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Consultas.ObtenerParticipanteDetalle;

public sealed record ObtenerParticipanteDetalleConsulta(Guid Id)
    : IRequest<PerfilParticipanteDto>;
