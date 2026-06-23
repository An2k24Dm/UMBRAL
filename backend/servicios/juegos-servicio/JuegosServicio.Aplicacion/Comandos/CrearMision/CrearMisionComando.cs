using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.CrearMision;

public sealed record CrearMisionComando(CrearMisionDto Dto, Guid CreadorId) : IRequest<Guid>;
