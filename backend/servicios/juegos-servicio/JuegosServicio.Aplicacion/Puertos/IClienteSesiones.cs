namespace JuegosServicio.Aplicacion.Puertos;

public interface IClienteSesiones
{
    Task<bool> ExisteSesionVigentePorMisionAsync(Guid misionId, CancellationToken cancelacion);
}
