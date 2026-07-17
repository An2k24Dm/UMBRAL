using MediatR;
using SesionesServicio.Commons.Dtos.ResultadosPuntaje;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerResultadoPuntaje;

public sealed record ObtenerResultadoPuntajeConsulta(Guid EventoId)
    : IRequest<ResultadoPuntajeDto>;
