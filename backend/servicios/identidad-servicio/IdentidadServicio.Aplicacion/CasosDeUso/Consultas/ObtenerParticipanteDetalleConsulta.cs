using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerParticipanteDetalleConsulta(Guid Id)
    : IRequest<PerfilParticipanteDto>;
