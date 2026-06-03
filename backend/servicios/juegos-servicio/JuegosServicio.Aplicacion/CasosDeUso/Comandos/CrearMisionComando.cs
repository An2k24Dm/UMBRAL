using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record CrearMisionComando(CrearMisionDto Dto, Guid CreadorId) : IRequest<Guid>;
