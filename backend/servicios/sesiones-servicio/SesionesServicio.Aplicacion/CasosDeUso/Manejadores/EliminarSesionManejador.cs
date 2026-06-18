using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

// HU39: elimina una sesión en estado Programada. Solo el Operador dueño puede
// hacerlo. Borra únicamente las filas locales del microservicio de sesiones;
// no toca juegos-servicio ni identidad-servicio.
public sealed class EliminarSesionManejador
    : IRequestHandler<EliminarSesionComando>
{
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;

    public EliminarSesionManejador(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
    }

    public async Task Handle(EliminarSesionComando comando, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para eliminar una sesión.");

        // Solo el Operador puede eliminar (Administrador y Participante quedan
        // fuera; el endpoint además aplica la política solo-Operador).
        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Solo un Operador puede eliminar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.Id, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        // El Operador solo puede eliminar las sesiones que él creó.
        var operadorId = _usuarioActual.ObtenerId() ?? Guid.Empty;
        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "No tiene permiso para eliminar esta sesión.");

        // Regla de dominio: solo se elimina si está Programada.
        sesion.ValidarPuedeEliminarse();

        await _repositorio.EliminarAsync(sesion, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }
}
