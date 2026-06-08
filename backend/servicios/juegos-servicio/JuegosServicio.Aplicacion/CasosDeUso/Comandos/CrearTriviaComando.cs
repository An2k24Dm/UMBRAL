using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record CrearTriviaComando(CrearTriviaDto Datos, Guid CreadorId)
    : IRequest<Guid>;

