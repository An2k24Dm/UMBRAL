using MediatR;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Fabricas;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Aplicacion.Comandos.ModificarSesion;

public sealed class ModificarSesionManejador
    : IRequestHandler<ModificarSesionComando, SesionDetalleDto>
{
    private const string RolOperador = "Operador";

    private readonly IValidador<ModificarSesionComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IValidadorMisionesSesion _validadorMisiones;
    private readonly IProveedorFechaHora _reloj;
    private readonly IFabricaSesion _fabricaSesion;
    private readonly FabricaMapeadorDetalleSesion _fabricaMapeador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarSesionManejador(
        IValidador<ModificarSesionComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IValidadorMisionesSesion validadorMisiones,
        IProveedorFechaHora reloj,
        IFabricaSesion fabricaSesion,
        FabricaMapeadorDetalleSesion fabricaMapeador,
        IRegistroLogsAplicacion registroLogs)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _validadorMisiones = validadorMisiones;
        _reloj = reloj;
        _fabricaSesion = fabricaSesion;
        _fabricaMapeador = fabricaMapeador;
        _registroLogs = registroLogs;
    }

    public async Task<SesionDetalleDto> Handle(
        ModificarSesionComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para modificar una sesión.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Solo un Operador puede modificar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.Id, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        var operadorId = _usuarioActual.ObtenerId() ?? Guid.Empty;
        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "No tiene permiso para modificar esta sesión.");

        if (sesion.Estado != EstadoSesion.Programada)
            throw new SesionNoModificableExcepcion(
                "Solo se pueden modificar sesiones en estado Programada.");

        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var fechaProgramada = comando.Datos.FechaProgramada.Kind == DateTimeKind.Utc
            ? comando.Datos.FechaProgramada
            : DateTime.SpecifyKind(comando.Datos.FechaProgramada, DateTimeKind.Utc);
        PoliticaProgramacionSesion.ValidarFechaProgramada(fechaProgramada, ahoraUtc);

        await _validadorMisiones.ValidarAsync(comando.Datos.MisionesIds, cancelacion);

        var sesionModificada = AplicarCambios(sesion, comando.Datos, fechaProgramada, ahoraUtc);

        await _repositorio.ActualizarAsync(sesionModificada, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "SesionModificada",
            descripcion: "Operador modificó una sesión correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesionModificada.Id,
                ["OperadorId"] = operadorId
            });

        return _fabricaMapeador.Mapear(sesionModificada);
    }

    private Sesion AplicarCambios(
        Sesion sesion, ModificarSesionDto datos, DateTime fechaProgramada, DateTime ahoraUtc)
    {
        var cambiaModo = !string.Equals(
            datos.Modo, sesion.TipoSesion, StringComparison.OrdinalIgnoreCase);

        if (!cambiaModo)
        {
            sesion.ModificarDatosBasicos(datos.Nombre, datos.Descripcion, fechaProgramada, ahoraUtc);
            sesion.ReemplazarMisiones(datos.MisionesIds);
            sesion.AplicarCapacidad(
                datos.MaximoParticipantes, datos.MaximoEquipos, datos.MaximoParticipantesPorEquipo);
            sesion.AplicarDuracion(datos.DuracionMinutosLimite);
            return sesion;
        }

        if (sesion.TieneInscritos)
            throw new SesionInvalidaExcepcion(
                "No se puede cambiar el tipo de una sesión que ya tiene participantes o equipos.");

        var datosReconstruccion = new DatosReconstruccionSesion(
            datos.Modo,
            sesion.Id,
            datos.Nombre,
            datos.Descripcion,
            fechaProgramada,
            sesion.CodigoAcceso,
            sesion.Estado,
            sesion.OperadorCreadorId,
            sesion.FechaCreacion,
            sesion.FechaInicioUtc,
            sesion.FechaFinalizacionUtc,
            datos.MisionesIds,
            datos.MaximoParticipantes,
            datos.MaximoEquipos,
            datos.MaximoParticipantesPorEquipo,
            datos.DuracionMinutosLimite);

        return _fabricaSesion.Reconstruir(datosReconstruccion);
    }
}
