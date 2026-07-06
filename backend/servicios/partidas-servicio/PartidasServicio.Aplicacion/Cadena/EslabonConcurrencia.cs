using PartidasServicio.Dominio.Abstract;

namespace PartidasServicio.Aplicacion.Cadena;

// Eslabón 3: verifica si el equipo/participante ya respondió esta pregunta.
// La unicidad real la garantiza el constraint de la DB; este eslabón permite
// devolver un DTO amigable sin lanzar una excepción de DB.
public sealed class EslabonConcurrencia : IEslabonValidacion
{
    private readonly IRepositorioRespuestas _repositorio;

    public EslabonConcurrencia(IRepositorioRespuestas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task ValidarAsync(ContextoValidacionRespuesta contexto, CancellationToken cancelacion)
    {
        bool yaRespondida;

        if (contexto.EquipoId.HasValue)
        {
            yaRespondida = await _repositorio.YaRespondioEquipoAsync(
                contexto.SesionId, contexto.PreguntaId, contexto.EquipoId.Value, cancelacion);
        }
        else
        {
            yaRespondida = await _repositorio.YaRespondioParticipanteAsync(
                contexto.SesionId, contexto.PreguntaId, contexto.ParticipanteId, cancelacion);
        }

        contexto.PreguntaYaRespondida = yaRespondida;
        // No lanza excepción: el manejador decide qué responder cuando ya fue respondida.
    }
}
