using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarTriviaComando(Guid TriviaId, ModificarTriviaDto Dto) : IRequest;
