using MediatR;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;

public sealed class IngresarSesionPorCodigoManejador
    : IRequestHandler<IngresarSesionPorCodigoComando, IngresarSesionRespuestaDto>
{
    private const string RolParticipante = "Participante";
    private const string MensajeSesionGrupal =
        "Esta sesión es grupal. Para ingresar debes crear un equipo o unirte a uno existente.";

    private readonly IValidador<IngresarSesionPorCodigoComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly PoliticaParticipacionUnicaSesion _participacionUnica;
    private readonly ConstructorRespuestaIngresoSesion _constructorRespuesta;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public IngresarSesionPorCodigoManejador(
        IValidador<IngresarSesionPorCodigoComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        PoliticaParticipacionUnicaSesion participacionUnica,
        ConstructorRespuestaIngresoSesion constructorRespuesta,
        INotificadorSesionesTiempoReal notificadorTiempoReal,
        IRegistroLogsAplicacion registroLogs)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _participacionUnica = participacionUnica;
        _constructorRespuesta = constructorRespuesta;
        _notificadorTiempoReal = notificadorTiempoReal;
        _registroLogs = registroLogs;
    }

    public async Task<IngresarSesionRespuestaDto> Handle(
        IngresarSesionPorCodigoComando comando,
        CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();
        var participanteId = ObtenerParticipanteActual();
        var codigo = comando.Datos.CodigoSesion.Trim().ToUpperInvariant();
        var sesion = await _repositorio.ObtenerPorCodigoAsync(codigo, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión indicada no existe.");

        if (sesion is SesionIndividual individual)
            return await IngresarEnSesionIndividualAsync(
                individual, participanteId, cancelacion);

        if (sesion is not SesionGrupal grupal)
            throw new SesionInvalidaExcepcion("El modo de la sesión no es válido.");

        var yaPertenecia = grupal.Equipos.Any(
            e => e.ContieneParticipanteIdentidadId(participanteId));
        return await _constructorRespuesta.ConstruirAsync(
            grupal,
            participanteId,
            ingresoRegistrado: false,
            yaPertenecia,
            yaPertenecia ? "Ya perteneces a un equipo en esta sesión." : MensajeSesionGrupal,
            cancelacion);
    }

    private async Task<IngresarSesionRespuestaDto> IngresarEnSesionIndividualAsync(
        SesionIndividual individual, Guid participanteId, CancellationToken cancelacion)
    {
        if (individual.Estado != EstadoSesion.EnPreparacion)
            throw new ParticipacionInvalidaExcepcion(
                "Solo puedes ingresar a una sesión en estado En Preparación.");

        var yaPertenecia = individual.Participantes.Any(
            p => p.ParticipanteIdentidadId == participanteId);

        if (!yaPertenecia)
        {
            await _participacionUnica.ValidarPuedeIngresarASesionAsync(
                participanteId, individual.Id, cancelacion);
            individual.AgregarParticipante(participanteId, _reloj.ObtenerFechaHoraUtc());
            await _repositorio.ActualizarAsync(individual, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
            await _notificadorTiempoReal.NotificarParticipantesSesionActualizadosAsync(
                individual.Id, cancelacion);

            _registroLogs.Informacion(
                evento: "ParticipanteIngresoSesionPorCodigo",
                descripcion: "Participante ingresó a una sesión por código correctamente",
                propiedades: new Dictionary<string, object?>
                {
                    ["SesionId"] = individual.Id,
                    ["ParticipanteId"] = participanteId
                });
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

    private Guid ObtenerParticipanteActual()
    {
        if (!_usuarioActual.EstaAutenticado() ||
            !_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante autenticado puede ingresar a una sesión.");

        return _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");
    }
}
