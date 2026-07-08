using MediatR;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.IngresarEquipo;

public sealed class IngresarEquipoManejador
    : IRequestHandler<IngresarEquipoComando, IngresarEquipoRespuestaDto>
{
    private const string RolParticipante = "Participante";

    private readonly IValidador<IngresarEquipoComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IHashContrasenaEquipo _hashContrasena;
    private readonly IProveedorFechaHora _reloj;
    private readonly PoliticaParticipacionUnicaSesion _participacionUnica;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public IngresarEquipoManejador(
        IValidador<IngresarEquipoComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IHashContrasenaEquipo hashContrasena,
        IProveedorFechaHora reloj,
        PoliticaParticipacionUnicaSesion participacionUnica,
        INotificadorSesionesTiempoReal notificadorTiempoReal,
        IRegistroLogsAplicacion registroLogs)
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
    }

    public async Task<IngresarEquipoRespuestaDto> Handle(
        IngresarEquipoComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para ingresar a un equipo.");

        if (!_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante puede ingresar a equipos.");

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        // Regla de participación única: bloquea si el participante ya está en
        // otra sesión activa, o si ya pertenece a esta misma sesión.
        await _participacionUnica.ValidarPuedeIngresarASesionAsync(
            participanteId, comando.SesionId, cancelacion);

        if (sesion is not SesionGrupal sesionGrupal)
            throw new EquipoInvalidoExcepcion(
                "Solo se puede ingresar a equipos de sesiones grupales.");

        if (sesionGrupal.Estado != EstadoSesion.EnPreparacion)
            throw new EquipoInvalidoExcepcion(
                "Solo puedes ingresar a un equipo cuando la sesión está en estado En Preparación.");

        var equipo = sesionGrupal.Equipos.FirstOrDefault(e => e.Id == comando.EquipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo solicitado no existe en esta sesión.");

        if (equipo.EstaLleno())
            throw new EquipoInvalidoExcepcion("El equipo no tiene cupos disponibles.");

        // Equipos públicos no requieren contraseña (se ignora la enviada).
        // Equipos privados exigen la contraseña configurada por el líder;
        // se verifica siempre contra el hash, nunca en texto plano.
        if (equipo.Tipo == TipoEquipo.Privado)
        {
            if (string.IsNullOrWhiteSpace(comando.Datos.Contrasena))
                throw new ExcepcionValidacion(
                    "Debes ingresar la contraseña del equipo.",
                    new[]
                    {
                        new ErrorValidacion(
                            "contrasena", "Debes ingresar la contraseña del equipo.")
                    });

            var coincide = _hashContrasena.Verificar(
                comando.Datos.Contrasena.Trim(), equipo.ContrasenaHash!.Valor);
            if (!coincide)
                throw new AccesoSesionNoPermitidoExcepcion(
                    "La contraseña del equipo es incorrecta.");
        }

        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();

        sesionGrupal.AgregarParticipanteAEquipo(
            comando.EquipoId, participanteId, ahoraUtc, ahoraUtc);

        await _repositorio.ActualizarAsync(sesionGrupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        // SignalR solo notifica que algo cambió, y siempre después de guardar.
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            sesionGrupal.Id, comando.EquipoId, cancelacion);
        await _notificadorTiempoReal.NotificarEquipoActualizadoAsync(
            sesionGrupal.Id, comando.EquipoId, cancelacion);
        // Refresca el conteo de participantes por equipo en el listado.
        await _notificadorTiempoReal.NotificarSesionActualizadaAsync(
            sesionGrupal.Id, sesionGrupal.Estado.ToString(), cancelacion);

        _registroLogs.Informacion(
            evento: "ParticipanteIngresoEquipo",
            descripcion: "Participante ingresó a un equipo correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesionGrupal.Id,
                ["EquipoId"] = comando.EquipoId,
                ["ParticipanteId"] = participanteId
            });

        return new IngresarEquipoRespuestaDto
        {
            SesionId = sesionGrupal.Id,
            EquipoId = equipo.Id,
            EquipoNombre = equipo.Nombre.Valor,
            Tipo = equipo.Tipo.ToString(),
            CantidadParticipantes = equipo.Participantes.Count,
            CapacidadMaxima = equipo.CapacidadMaxima,
            EsMiEquipo = true
        };
    }
}
