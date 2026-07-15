namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

public abstract class EslabonValidacionEvidenciaTesoroBase
    : IEslabonValidacionEvidenciaTesoro
{
    private IEslabonValidacionEvidenciaTesoro? _siguiente;

    public IEslabonValidacionEvidenciaTesoro EstablecerSiguiente(
        IEslabonValidacionEvidenciaTesoro siguiente)
    {
        _siguiente = siguiente;
        return siguiente;
    }

    public async Task ManejarAsync(
        ContextoValidacionEvidenciaTesoro contexto,
        CancellationToken cancelacion)
    {
        await ProcesarAsync(contexto, cancelacion);

        if (_siguiente is not null)
            await _siguiente.ManejarAsync(contexto, cancelacion);
    }

    protected abstract Task ProcesarAsync(
        ContextoValidacionEvidenciaTesoro contexto,
        CancellationToken cancelacion);
}
