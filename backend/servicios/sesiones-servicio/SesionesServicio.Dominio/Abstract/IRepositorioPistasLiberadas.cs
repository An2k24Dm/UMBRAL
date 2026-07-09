namespace SesionesServicio.Dominio.Abstract;

public sealed record PistaLiberadaRegistro(
    Guid SesionId,
    Guid EtapaId,
    Guid? PistaId,
    string Contenido,
    DateTime FechaLiberacionUtc);

public interface IRepositorioPistasLiberadas
{
    Task AgregarAsync(PistaLiberadaRegistro registro, CancellationToken cancelacion);
    Task<bool> ExistePistaLiberadaAsync(Guid sesionId, Guid etapaId, Guid pistaId, CancellationToken cancelacion);
    Task<List<PistaLiberadaRegistro>> ObtenerPorEtapaAsync(Guid sesionId, Guid etapaId, CancellationToken cancelacion);
}
