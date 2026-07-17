namespace SesionesServicio.Aplicacion.Puertos;

public interface IServicioFinalizacionSesion
{
    Task FinalizarSiTodasEtapasCompletadasAsync(
        Guid sesionId, Guid etapaIdCompletada, CancellationToken cancelacion);

    Task AvanzarEtapaPorVencimientoAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);

    Task FinalizarSesionPorVencimientoAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task<bool> FinalizarSesionSiDuracionVencidaAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task ActivarEtapaProgramadaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);

    // Todos completaron la etapa: en vez de cerrar de inmediato, entra en
    // CierrePendiente para mostrar el feedback final unos segundos. El worker la
    // cierra al vencer. No emite EtapaCompletada/EtapaPorComenzar todavía.
    Task ProgramarCierreTrasFeedbackAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);

    // Cierra realmente una etapa en CierrePendiente cuando su feedback final
    // venció: registra completada + programa la siguiente (o finaliza) y recién
    // entonces emite EtapaCompletada + EtapaPorComenzar (o SesionActualizada).
    Task CerrarEtapaTrasCierrePendienteAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);
}
