using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarTriviaManejador : IRequestHandler<DesactivarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IClienteSesiones _clienteSesiones;

    public DesactivarTriviaManejador(
        IRepositorioJuegos repositorio,
        IClienteSesiones clienteSesiones)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
    }

    public async Task Handle(DesactivarTriviaComando comando, CancellationToken cancelacion)
    {
        // 1. Resolver el agregado. Si no existe, 404 vía middleware.
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        // 2. Antes de tocar el estado de dominio, consultar a sesiones-
        //    servicio: si hay una sesión vigente para esta trivia, no
        //    podemos archivarla porque dejaríamos sesiones huérfanas.
        //    Esta verificación va ANTES de Desactivar() para que no se
        //    persista nada si la regla se incumple.
        var tieneSesionesVigentes = await _clienteSesiones
            .ExisteSesionVigentePorContenidoAsync(TipoJuego.Trivia, trivia.Id, cancelacion);
        if (tieneSesionesVigentes)
            throw new ContenidoConSesionesVigentesExcepcion(TipoJuego.Trivia, trivia.Id);

        // 3. Transición de estado en el dominio y persistencia.
        trivia.Desactivar();
        await _repositorio.ArchivarTriviaAsync(trivia, cancelacion);
    }
}
