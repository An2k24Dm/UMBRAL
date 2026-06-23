using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarMision;

public sealed record ModificarMisionComando(Guid MisionId, ModificarMisionDto Dto) : IRequest;
