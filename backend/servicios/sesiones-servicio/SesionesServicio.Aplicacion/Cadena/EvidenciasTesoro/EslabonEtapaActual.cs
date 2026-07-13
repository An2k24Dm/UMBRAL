using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

// Eslabón 3: valida que la etapa actual corresponda (misión, etapa, tipo
// BusquedaTesoro y búsqueda) y que el participante/equipo pueda ejecutarla.
// Solo orquesta el servicio existente; no duplica su lógica. Si la validación
// falla, el servicio lanza su excepción y la cadena se detiene.
public sealed class EslabonEtapaActual : EslabonValidacionEvidenciaTesoroBase
{
    private readonly IServicioProgresoSecuencialSesion _servicioProgresoSecuencial;

    public EslabonEtapaActual(IServicioProgresoSecuencialSesion servicioProgresoSecuencial)
        => _servicioProgresoSecuencial = servicioProgresoSecuencial;

    protected override Task ProcesarAsync(
        ContextoValidacionEvidenciaTesoro contexto, CancellationToken cancelacion)
        => _servicioProgresoSecuencial.ValidarEtapaActualAsync(
            contexto.Sesion!,
            contexto.ParticipanteIdentidadId,
            contexto.MisionId,
            contexto.EtapaId,
            "BusquedaTesoro",
            contexto.BusquedaId,
            cancelacion);
}
