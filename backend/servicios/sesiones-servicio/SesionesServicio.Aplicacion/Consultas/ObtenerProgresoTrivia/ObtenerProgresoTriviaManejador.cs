using MediatR;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoTrivia;

public sealed class ObtenerProgresoTriviaManejador
    : IRequestHandler<ObtenerProgresoTriviaConsulta, IReadOnlyList<ProgresoTriviaParticipanteDto>>
{
    private readonly IRepositorioRespuestasTrivia _repositorioRespuestas;

    public ObtenerProgresoTriviaManejador(IRepositorioRespuestasTrivia repositorioRespuestas)
    {
        _repositorioRespuestas = repositorioRespuestas;
    }

    public async Task<IReadOnlyList<ProgresoTriviaParticipanteDto>> Handle(
        ObtenerProgresoTriviaConsulta consulta, CancellationToken cancelacion)
    {
        var items = await _repositorioRespuestas
            .ObtenerProgresoTriviaAsync(consulta.SesionId, cancelacion);

        return items
            .Select(i => new ProgresoTriviaParticipanteDto
            {
                ParticipanteIdentidadId = i.ParticipanteIdentidadId,
                TotalRespondidas = i.TotalRespondidas,
                Correctas = i.Correctas,
                Incorrectas = i.TotalRespondidas - i.Correctas,
                PuntosGanados = i.PuntosGanados
            })
            .ToList()
            .AsReadOnly();
    }
}
