using MediatR;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.IngresarSesionIndividual;

// Caso de uso CQRS: ingreso individual por id de sesión. El flujo vive de
// forma explícita en el manejador (sin servicio de aplicación intermedio).
public sealed class IngresarSesionIndividualManejador
    : IRequestHandler<IngresarSesionIndividualComando, IngresarSesionRespuestaDto>
{
    private const string RolParticipante = "Participante";
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly PoliticaParticipacionUnicaSesion _participacionUnica;
    private readonly ConstructorRespuestaIngresoSesion _constructorRespuesta;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;

    public IngresarSesionIndividualManejador(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        PoliticaParticipacionUnicaSesion participacionUnica,
        ConstructorRespuestaIngresoSesion constructorRespuesta,
        INotificadorSesionesTiempoReal notificadorTiempoReal)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _participacionUnica = participacionUnica;
        _constructorRespuesta = constructorRespuesta;
        _notificadorTiempoReal = notificadorTiempoReal;
    }

    public async Task<IngresarSesionRespuestaDto> Handle(
        IngresarSesionIndividualComando comando,
        CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado() ||
            !_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante autenticado puede ingresar a una sesión.");

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");
        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión indicada no existe.");

        if (sesion is not SesionIndividual individual)
            throw new ParticipacionInvalidaExcepcion(
                "Para ingresar a una sesión grupal debes crear o unirte a un equipo.");

        if (individual.Estado != EstadoSesion.EnPreparacion)
            throw new ParticipacionInvalidaExcepcion(
                "Solo puedes ingresar a una sesión en estado En Preparación.");

        var yaPertenecia = individual.Participantes.Any(
            p => p.ParticipanteIdentidadId == participanteId);

        if (!yaPertenecia)
        {
            // Solo cuando hay ingreso real persistimos y, recién después de
            // guardar, notificamos por tiempo real (el puerto, no SignalR).
            await _participacionUnica.ValidarPuedeIngresarASesionAsync(
                participanteId, individual.Id, cancelacion);
            individual.AgregarParticipante(participanteId, _reloj.ObtenerFechaHoraUtc());
            await _repositorio.ActualizarAsync(individual, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
            await _notificadorTiempoReal.NotificarParticipantesSesionActualizadosAsync(
                individual.Id, cancelacion);
        }

        return await _constructorRespuesta.ConstruirAsync(
            individual,
            participanteId,
            ingresoRegistrado: true,
            yaPertenecia,
            yaPertenecia
                ? "Ya ingresaste a esta sesión."
                : "Ingreso registrado correctamente.",
            cancelacion);
    }
}
