using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.CrearTrivia;

public sealed record CrearTriviaComando(CrearTriviaDto Datos, Guid CreadorId)
    : IRequest<Guid>;

