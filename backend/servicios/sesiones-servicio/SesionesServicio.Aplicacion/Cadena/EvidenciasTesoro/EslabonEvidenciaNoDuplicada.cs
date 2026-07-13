using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

// Eslabón 4: comprueba que el jugador lógico no haya completado ya válidamente
// esta etapa. Individual: por participante; Grupal: por equipo (si cualquier
// integrante consiguió evidencia válida, el equipo queda completado y otro
// integrante ya no puede generar otra evidencia válida puntuable). Si ya existe,
// lanza la excepción de duplicado y detiene la cadena. Regla idéntica al flujo
// original; la restricción única filtrada en BD sigue siendo la autoridad final.
public sealed class EslabonEvidenciaNoDuplicada : EslabonValidacionEvidenciaTesoroBase
{
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidencias;

    public EslabonEvidenciaNoDuplicada(IRepositorioEvidenciasTesoro repositorioEvidencias)
        => _repositorioEvidencias = repositorioEvidencias;

    protected override async Task ProcesarAsync(
        ContextoValidacionEvidenciaTesoro contexto, CancellationToken cancelacion)
    {
        var yaCompletado = contexto.EquipoId.HasValue
            ? await _repositorioEvidencias.ExisteEvidenciaValidaEquipoAsync(
                contexto.SesionId, contexto.EtapaId, contexto.EquipoId.Value, cancelacion)
            : await _repositorioEvidencias.ExisteEvidenciaValidaIndividualAsync(
                contexto.SesionId, contexto.EtapaId, contexto.ParticipanteIdentidadId, cancelacion);

        if (yaCompletado)
            throw new EvidenciaTesoroDuplicadaExcepcion(esEquipo: contexto.EquipoId.HasValue);
    }
}
