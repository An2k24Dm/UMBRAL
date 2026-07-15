namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

public sealed class FabricaCadenaValidacionEvidenciaTesoro
{
    private readonly EslabonSesionActiva _sesionActiva;
    private readonly EslabonParticipanteInscrito _participanteInscrito;
    private readonly EslabonEtapaActual _etapaActual;
    private readonly EslabonEvidenciaNoDuplicada _evidenciaNoDuplicada;
    private readonly EslabonCodigoQr _codigoQr;

    public FabricaCadenaValidacionEvidenciaTesoro(
        EslabonSesionActiva sesionActiva,
        EslabonParticipanteInscrito participanteInscrito,
        EslabonEtapaActual etapaActual,
        EslabonEvidenciaNoDuplicada evidenciaNoDuplicada,
        EslabonCodigoQr codigoQr)
    {
        _sesionActiva = sesionActiva;
        _participanteInscrito = participanteInscrito;
        _etapaActual = etapaActual;
        _evidenciaNoDuplicada = evidenciaNoDuplicada;
        _codigoQr = codigoQr;
    }

    public IEslabonValidacionEvidenciaTesoro Crear()
    {
        _sesionActiva
            .EstablecerSiguiente(_participanteInscrito)
            .EstablecerSiguiente(_etapaActual)
            .EstablecerSiguiente(_evidenciaNoDuplicada)
            .EstablecerSiguiente(_codigoQr);

        return _sesionActiva;
    }
}
