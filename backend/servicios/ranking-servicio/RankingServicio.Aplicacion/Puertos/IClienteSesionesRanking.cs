namespace RankingServicio.Aplicacion.Puertos;

// Cliente hacia sesiones-servicio para enriquecer, solo al consultar, el nombre
// de los equipos de una sesión. Ranking no almacena nombres; los obtiene por id.
public interface IClienteSesionesRanking
{
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresEquiposAsync(
        Guid sesionId,
        CancellationToken cancelacion);
}
