using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Consultas.ConsultarParticipantesPorIds;

public sealed class ConsultarParticipantesPorIdsManejador
    : IRequestHandler<ConsultarParticipantesPorIdsConsulta, IReadOnlyList<ParticipanteBasicoDto>>
{
    private readonly IRepositorioUsuariosLectura _repositorio;

    public ConsultarParticipantesPorIdsManejador(IRepositorioUsuariosLectura repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IReadOnlyList<ParticipanteBasicoDto>> Handle(
        ConsultarParticipantesPorIdsConsulta consulta, CancellationToken cancelacion)
    {
        if (consulta.ParticipantesIds is null || consulta.ParticipantesIds.Count == 0)
            return Array.Empty<ParticipanteBasicoDto>();

        var filas = await _repositorio.ObtenerParticipantesBasicosPorIdsKeycloakAsync(
            consulta.ParticipantesIds, cancelacion);

        return filas
            .Select(f => new ParticipanteBasicoDto
            {
                Id = f.Id,
                Nombre = f.Nombre,
                Apellido = f.Apellido,
                Alias = f.Alias
            })
            .ToList();
    }
}
