using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

// Eslabón 5 (último): valida el código QR contra juegos-servicio. Un QR
// incorrecto NO es un error estructural: es un resultado válido de evaluación
// (EsCodigoQrValido = false) que luego se persiste como evidencia inválida
// (0 puntos, no completa etapa, se puede reintentar). Solo se detiene la cadena
// si la búsqueda no existe (el cliente devuelve null). Sesiones nunca conoce ni
// compara el código esperado: juegos-servicio sigue siendo el único que valida.
public sealed class EslabonCodigoQr : EslabonValidacionEvidenciaTesoroBase
{
    private readonly IClienteBusquedaTesoro _clienteTesoro;

    public EslabonCodigoQr(IClienteBusquedaTesoro clienteTesoro)
        => _clienteTesoro = clienteTesoro;

    protected override async Task ProcesarAsync(
        ContextoValidacionEvidenciaTesoro contexto, CancellationToken cancelacion)
    {
        var esValida = await _clienteTesoro.ValidarCodigoQrAsync(
            contexto.BusquedaId, contexto.CodigoEscaneado, cancelacion)
            ?? throw new InvalidOperationException("Búsqueda del tesoro no encontrada.");

        contexto.EsCodigoQrValido = esValida;
    }
}
