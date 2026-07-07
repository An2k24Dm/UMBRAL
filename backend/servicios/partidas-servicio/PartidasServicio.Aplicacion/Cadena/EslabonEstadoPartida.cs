using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Aplicacion.Cadena;

// Eslabón 1 (después de EslabonEstadoSesion): garantiza que la Partida exista y esté Iniciada.
// Si la sesión ya está Activa pero no se creó la Partida explícitamente, la crea aquí (lazy init).
public sealed class EslabonEstadoPartida : IEslabonValidacion
{
    private readonly IRepositorioPartidas _repositorio;
    private readonly IServicioPartidas _servicioPartidas;

    public EslabonEstadoPartida(IRepositorioPartidas repositorio, IServicioPartidas servicioPartidas)
    {
        _repositorio = repositorio;
        _servicioPartidas = servicioPartidas;
    }

    public async Task ValidarAsync(ContextoValidacionRespuesta contexto, CancellationToken cancelacion)
    {
        var partida = await _repositorio.ObtenerPorSesionIdAsync(contexto.SesionId, cancelacion);

        if (partida is null)
        {
            // La sesión ya fue validada como Activa por EslabonEstadoSesion.
            // Creamos e iniciamos la partida en el primer intento de respuesta.
            await _servicioPartidas.IniciarPartidaAsync(contexto.SesionId, cancelacion);
            return;
        }

        if (!partida.EstaActiva)
            throw new SesionNoActivaExcepcion(partida.NombreEstado);
    }
}
