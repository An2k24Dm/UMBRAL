using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarTrivia;

public sealed record ModificarTriviaComando(Guid TriviaId, ModificarTriviaDto Dto) : IRequest;
