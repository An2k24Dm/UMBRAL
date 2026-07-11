namespace SesionesServicio.Aplicacion.Puertos;

public interface IServicioGruposSesionesTiempoReal
{
    Task UnirseAListadoAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        CancellationToken cancelacion);

    Task SalirDeListadoAsync(
        string connectionId, CancellationToken cancelacion);

    Task UnirseASesionAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        Guid sesionId, CancellationToken cancelacion);

    Task SalirDeSesionAsync(
        string connectionId, Guid sesionId, CancellationToken cancelacion);

    Task UnirseAEquipoAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        Guid equipoId, CancellationToken cancelacion);

    Task SalirDeEquipoAsync(
        string connectionId, Guid equipoId, CancellationToken cancelacion);
}
