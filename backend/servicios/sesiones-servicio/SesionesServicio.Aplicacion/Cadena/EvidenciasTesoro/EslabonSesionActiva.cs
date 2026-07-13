using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

// Eslabón 1: carga la sesión, verifica que exista y que esté activa.
// Detiene la cadena (lanza) si no existe o no está activa; si pasa, deja la
// sesión en el contexto para reutilizarla en los siguientes eslabones.
public sealed class EslabonSesionActiva : EslabonValidacionEvidenciaTesoroBase
{
    private readonly IRepositorioSesiones _repositorioSesiones;

    public EslabonSesionActiva(IRepositorioSesiones repositorioSesiones)
        => _repositorioSesiones = repositorioSesiones;

    protected override async Task ProcesarAsync(
        ContextoValidacionEvidenciaTesoro contexto, CancellationToken cancelacion)
    {
        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(contexto.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new OperacionSesionInvalidaExcepcion(
                $"La sesion no esta activa. Estado actual: {sesion.Estado}.");

        contexto.Sesion = sesion;
    }
}
