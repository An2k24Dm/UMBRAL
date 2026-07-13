using MediatR;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Comandos.CrearEquipo;

public sealed class CrearEquipoManejador
    : IRequestHandler<CrearEquipoComando, CrearEquipoRespuestaDto>
{
    private const string RolParticipante = "Participante";

    private readonly IValidador<CrearEquipoComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IHashContrasenaEquipo _hashContrasena;
    private readonly IProveedorFechaHora _reloj;
    private readonly PoliticaParticipacionUnicaSesion _participacionUnica;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;
    private readonly IPublicadorEventosRanking _publicadorRanking;

    public CrearEquipoManejador(
        IValidador<CrearEquipoComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IHashContrasenaEquipo hashContrasena,
        IProveedorFechaHora reloj,
        PoliticaParticipacionUnicaSesion participacionUnica,
        INotificadorSesionesTiempoReal notificadorTiempoReal,
        IRegistroLogsAplicacion registroLogs,
        IPublicadorEventosRanking publicadorRanking)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _hashContrasena = hashContrasena;
        _reloj = reloj;
        _participacionUnica = participacionUnica;
        _notificadorTiempoReal = notificadorTiempoReal;
        _registroLogs = registroLogs;
        _publicadorRanking = publicadorRanking;
    }

    public async Task<CrearEquipoRespuestaDto> Handle(
        CrearEquipoComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para crear un equipo.");

        if (!_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante puede crear equipos.");

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        await _participacionUnica.ValidarPuedeIngresarASesionAsync(
            participanteId, comando.SesionId, cancelacion);

        if (sesion is not SesionGrupal sesionGrupal)
            throw new EquipoInvalidoExcepcion(
                "Solo se pueden crear equipos en sesiones grupales.");

        if (sesionGrupal.Estado != EstadoSesion.EnPreparacion)
            throw new EquipoInvalidoExcepcion(
                "Solo se pueden crear equipos en sesiones en estado En Preparación.");

        var nombre = NombreEquipo.Crear(comando.Datos.Nombre);
        var tipo = comando.Datos.Tipo == TipoEquipoDto.Privado
            ? TipoEquipo.Privado
            : TipoEquipo.Publico;

        ContrasenaEquipoHash? contrasenaHash = null;
        if (tipo == TipoEquipo.Privado)
        {
            var hash = _hashContrasena.Hashear(comando.Datos.Contrasena!.Trim());
            contrasenaHash = ContrasenaEquipoHash.Crear(hash);
        }

        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();

        var equipo = sesionGrupal.CrearEquipo(
            nombre, tipo, contrasenaHash, participanteId, ahoraUtc, ahoraUtc);

        await _repositorio.ActualizarAsync(sesionGrupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            sesionGrupal.Id, equipo.Id, cancelacion);
        await _notificadorTiempoReal.NotificarEquipoActualizadoAsync(
            sesionGrupal.Id, equipo.Id, cancelacion);
        // Refresca el conteo de equipos en el listado (web y móvil).
        await _notificadorTiempoReal.NotificarSesionActualizadaAsync(
            sesionGrupal.Id, sesionGrupal.Estado.ToString(), cancelacion);

        _registroLogs.Informacion(
            evento: "EquipoCreado",
            descripcion: "Participante creó un equipo correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesionGrupal.Id,
                ["EquipoId"] = equipo.Id,
                ["ParticipanteId"] = participanteId
            });

        await _publicadorRanking.PublicarEquipoCreadoSesionAsync(
            sesionGrupal.Id, equipo.Id, cancelacion);

        return new CrearEquipoRespuestaDto
        {
            Id = equipo.Id,
            SesionId = equipo.SesionId,
            Nombre = equipo.Nombre.Valor,
            Tipo = equipo.Tipo.ToString(),
            CapacidadMaxima = equipo.CapacidadMaxima,
            CantidadParticipantes = equipo.Participantes.Count,
            LiderParticipanteId = equipo.LiderParticipanteId,
            Puntaje = equipo.Puntaje.Valor,
            FechaCreacion = equipo.FechaCreacion
        };
    }
}
