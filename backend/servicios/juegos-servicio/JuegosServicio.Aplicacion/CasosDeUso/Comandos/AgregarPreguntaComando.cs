using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record AgregarPreguntaComando(Guid TriviaId, AgregarPreguntaDto Datos)
    : IRequest<Guid>;
