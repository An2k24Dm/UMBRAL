using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Comandos.ModificarEquipo;

// HU41 — Modificar un equipo de una sesión grupal En Preparación. Solo el
// líder del equipo puede hacerlo. El dominio protege las invariantes; aquí
// se orquesta la autorización por rol y el hasheo de la contraseña.
public sealed class ModificarEquipoManejador
    : IRequestHandler<ModificarEquipoComando, ModificarEquipoRespuestaDto>
{
    private const string RolParticipante = "Participante";

    private readonly IValidador<ModificarEquipoComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IHashContrasenaEquipo _hashContrasena;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarEquipoManejador(
        IValidador<ModificarEquipoComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IHashContrasenaEquipo hashContrasena,
        INotificadorSesionesTiempoReal notificadorTiempoReal,
        IRegistroLogsAplicacion registroLogs)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _hashContrasena = hashContrasena;
        _notificadorTiempoReal = notificadorTiempoReal;
        _registroLogs = registroLogs;
    }

    public async Task<ModificarEquipoRespuestaDto> Handle(
        ModificarEquipoComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para modificar un equipo.");

        if (!_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante puede modificar equipos.");

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionGrupal sesionGrupal)
            throw new SesionNoGrupalExcepcion(
                "Solo se pueden modificar equipos de sesiones grupales.");

        var equipoActual = sesionGrupal.Equipos.FirstOrDefault(e => e.Id == comando.EquipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo solicitado no existe en esta sesión.");

        var nombre = NombreEquipo.Crear(comando.Datos.Nombre);
        var nuevoTipo = comando.Datos.Tipo == TipoEquipoDto.Privado
            ? Dominio.Enums.TipoEquipo.Privado
            : Dominio.Enums.TipoEquipo.Publico;

        var (contrasenaHash, actualizarContrasena) = ResolverContrasena(
            comando.Datos, nuevoTipo, equipoActual);

        var equipo = sesionGrupal.ModificarEquipo(
            comando.EquipoId, participanteId, nombre, nuevoTipo,
            contrasenaHash, actualizarContrasena);

        await _repositorio.ActualizarAsync(sesionGrupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            sesionGrupal.Id, equipo.Id, cancelacion);
        await _notificadorTiempoReal.NotificarEquipoActualizadoAsync(
            sesionGrupal.Id, equipo.Id, cancelacion);
        await _notificadorTiempoReal.NotificarSesionActualizadaAsync(
            sesionGrupal.Id, sesionGrupal.Estado.ToString(), cancelacion);

        _registroLogs.Informacion(
            evento: "EquipoModificado",
            descripcion: "Participante modificó un equipo correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesionGrupal.Id,
                ["EquipoId"] = equipo.Id,
                ["ParticipanteId"] = participanteId
            });

        return new ModificarEquipoRespuestaDto
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

    // Decide el hash a aplicar. La contraseña en texto plano solo existe aquí.
    private (ContrasenaEquipoHash? Hash, bool Actualizar) ResolverContrasena(
        ModificarEquipoDto datos, Dominio.Enums.TipoEquipo nuevoTipo, Equipo equipoActual)
    {
        if (nuevoTipo == Dominio.Enums.TipoEquipo.Publico)
            return (null, false);

        var tieneContrasena = !string.IsNullOrWhiteSpace(datos.Contrasena);
        if (tieneContrasena)
        {
            var hash = _hashContrasena.Hashear(datos.Contrasena!.Trim());
            return (ContrasenaEquipoHash.Crear(hash), true);
        }

        // Privado sin contraseña enviada: solo válido si el equipo ya era
        // privado (conserva su hash). Si venía de público, es obligatoria.
        if (equipoActual.Tipo == Dominio.Enums.TipoEquipo.Publico)
            throw new ExcepcionValidacion(
                "Datos inválidos.",
                new[]
                {
                    new ErrorValidacion(
                        "contrasena",
                        "Debes indicar una contraseña para un equipo privado.")
                });

        return (null, false);
    }
}
