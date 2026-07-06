using MediatR;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;

public sealed record EnviarRespuestaTriviaComando(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid TriviaId,
    EnviarRespuestaTriviaDto Dto) : IRequest<RespuestaTriviaResultadoDto>;
