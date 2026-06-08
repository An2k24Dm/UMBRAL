using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioOperadores
{
    Task<Operador?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);
    Task AgregarAsync(
        Operador operador, string idKeycloak, CancellationToken cancelacion);
    Task<string> ActualizarAsync(Operador operador, CancellationToken cancelacion);
    Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion);
    Task EliminarAsync(Operador operador, CancellationToken cancelacion);
    Task<string?> ObtenerIdKeycloakAsync(Guid idOperador, CancellationToken cancelacion);
    Task ActualizarEstadoAsync(Operador operador, CancellationToken cancelacion);
}
