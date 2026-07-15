using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;

public sealed class ObtenerTriviaParticipanteManejador
    : IRequestHandler<ObtenerTriviaParticipanteConsulta, TriviaParticipanteJuegosDto?>
{
    private const string TipoTrivia = "Trivia";

    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IClienteJuegosTrivia _clienteTrivia;
    private readonly IServicioProgresoSecuencialSesion _servicioProgresoSecuencial;

    public ObtenerTriviaParticipanteManejador(
        IUsuarioActual usuario,
        IRepositorioSesiones repositorioSesiones,
        IClienteJuegosTrivia clienteTrivia,
        IServicioProgresoSecuencialSesion servicioProgresoSecuencial)
    {
        _usuario = usuario;
        _repositorioSesiones = repositorioSesiones;
        _clienteTrivia = clienteTrivia;
        _servicioProgresoSecuencial = servicioProgresoSecuencial;
    }

    public async Task<TriviaParticipanteJuegosDto?> Handle(
        ObtenerTriviaParticipanteConsulta consulta, CancellationToken cancelacion)
    {
        var participanteIdentidadId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(consulta.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");

        await _servicioProgresoSecuencial.ValidarEtapaActualAsync(
            sesion,
            participanteIdentidadId,
            consulta.MisionId,
            consulta.EtapaId,
            TipoTrivia,
            consulta.TriviaId,
            cancelacion);

        return await _clienteTrivia.ObtenerTriviaParticipanteAsync(consulta.TriviaId, cancelacion);
    }
}
