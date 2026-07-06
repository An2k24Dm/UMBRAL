namespace PartidasServicio.Aplicacion.Cadena;

public interface IEslabonValidacion
{
    Task ValidarAsync(ContextoValidacionRespuesta contexto, CancellationToken cancelacion);
}
