namespace SesionesServicio.Aplicacion.Puertos;

public interface IClienteIdentidadUsuarios
{
    Task<bool> EsAdministradorAsync(
        Guid usuarioId, CancellationToken cancelacion);
    Task<IReadOnlyCollection<Guid>> FiltrarAdministradoresAsync(
        IReadOnlyCollection<Guid> usuariosIds, CancellationToken cancelacion);
}
