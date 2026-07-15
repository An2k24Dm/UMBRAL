using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

// Eslabón 2: comprueba que el usuario pertenece a la sesión y obtiene su
// Participante, su EquipoId (grupal) y el total de competidores.
// Individual: se busca en SesionIndividual.Participantes; total = nº participantes.
// Grupal: se busca dentro de SesionGrupal.Equipos → Participantes; total = nº equipos.
// Regla y excepción idénticas a las del manejador original.
public sealed class EslabonParticipanteInscrito : EslabonValidacionEvidenciaTesoroBase
{
    protected override Task ProcesarAsync(
        ContextoValidacionEvidenciaTesoro contexto, CancellationToken cancelacion)
    {
        // La sesión ya fue cargada y validada por el eslabón anterior.
        var sesion = contexto.Sesion!;

        var (participante, totalCompetidores) =
            ObtenerJugador(sesion, contexto.ParticipanteIdentidadId);

        contexto.Participante = participante;
        contexto.EquipoId = participante.EquipoId;
        contexto.TotalCompetidores = totalCompetidores;

        return Task.CompletedTask;
    }

    private static (Participante participante, int totalCompetidores) ObtenerJugador(
        Sesion sesion, Guid participanteId)
    {
        if (sesion is SesionIndividual individual)
        {
            var p = individual.Participantes
                .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteId)
                ?? throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return (p, individual.Participantes.Count);
        }

        if (sesion is SesionGrupal grupal)
        {
            foreach (var equipo in grupal.Equipos)
            {
                var p = equipo.Participantes
                    .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteId);
                if (p is not null)
                    return (p, grupal.Equipos.Count);
            }

            throw new ParticipacionInvalidaExcepcion(
                "El participante no esta inscrito en esta sesion.");
        }

        throw new SesionInvalidaExcepcion("Tipo de sesion no soportado.");
    }
}
