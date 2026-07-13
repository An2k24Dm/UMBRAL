namespace RankingServicio.Aplicacion.Puertos;

// Cliente hacia sesiones-servicio para enriquecer, solo al consultar, el nombre
// actual de los equipos de una sesión a partir de su identificador. El nombre
// del equipo es propiedad de sesiones-servicio; ranking no lo almacena.
public interface IClienteSesionesRanking
{
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresEquiposAsync(
        Guid sesionId,
        CancellationToken cancelacion);
}
