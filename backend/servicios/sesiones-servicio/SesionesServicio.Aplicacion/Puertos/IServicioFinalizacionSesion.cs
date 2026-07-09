namespace SesionesServicio.Aplicacion.Puertos;

public interface IServicioFinalizacionSesion
{
    Task FinalizarSiTodasEtapasCompletadasAsync(
        Guid sesionId, Guid etapaIdCompletada, CancellationToken cancelacion);
}
