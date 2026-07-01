using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarEquipoSesionGrupal;

// HU44 — El Operador creador expulsa un equipo completo de su sesión grupal.
// Al quitar el equipo, sus integrantes dejan de pertenecer a la sesión. El
// dominio protege la regla de estado; aquí se orquesta la autorización
// (Operador dueño), la persistencia y la notificación en tiempo real.
public sealed class ExpulsarEquipoSesionGrupalManejador
    : IRequestHandler<ExpulsarEquipoSesionGrupalComando>
{
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;

    public ExpulsarEquipoSesionGrupalManejador(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        INotificadorSesionesTiempoReal notificadorTiempoReal)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _notificadorTiempoReal = notificadorTiempoReal;
    }

    public async Task Handle(
        ExpulsarEquipoSesionGrupalComando comando,
        CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para expulsar equipos.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Operador puede expulsar participantes o equipos.");

        var operadorId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del operador.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionGrupal grupal)
            throw new SesionNoGrupalExcepcion(
                "Solo se pueden expulsar equipos de sesiones grupales.");

        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el Operador creador de la sesión puede expulsar participantes o equipos.");

        // Capturamos nombre e integrantes del equipo antes de quitarlo, para
        // avisar a cada integrante por SignalR luego de persistir. La
        // validación de estado/existencia ocurre dentro de ExpulsarEquipo.
        var equipo = grupal.Equipos.FirstOrDefault(e => e.Id == comando.EquipoId);
        var equipoNombre = equipo?.Nombre.Valor ?? string.Empty;
        var integrantesIdentidadIds = equipo?.Participantes
            .Select(p => p.ParticipanteIdentidadId)
            .ToList() ?? new List<Guid>();

        grupal.ExpulsarEquipo(comando.EquipoId);

        await _repositorio.ActualizarAsync(grupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        // SignalR solo notifica que algo cambió, y siempre después de guardar.
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            grupal.Id, comando.EquipoId, cancelacion);
        await _notificadorTiempoReal.NotificarEquipoExpulsadoAsync(
            integrantesIdentidadIds, grupal.Id, comando.EquipoId, equipoNombre, cancelacion);
    }
}
