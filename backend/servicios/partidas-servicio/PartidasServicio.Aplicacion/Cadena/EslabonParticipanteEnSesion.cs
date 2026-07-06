using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Aplicacion.Cadena;

// Eslabón 2: verifica que el participante esté inscrito en la sesión.
// El EquipoId ya fue poblado por EslabonEstadoSesion desde el mismo response.
public sealed class EslabonParticipanteEnSesion : IEslabonValidacion
{
    public Task ValidarAsync(ContextoValidacionRespuesta contexto, CancellationToken cancelacion)
    {
        if (!contexto.ParticipanteInscrito)
            throw new ParticipanteNoEnSesionExcepcion();

        return Task.CompletedTask;
    }
}
