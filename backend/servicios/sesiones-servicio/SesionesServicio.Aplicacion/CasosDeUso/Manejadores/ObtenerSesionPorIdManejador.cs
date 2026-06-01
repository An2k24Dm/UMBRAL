using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

// HU34 — Detalle de sesión con visibilidad por rol y enriquecimiento
// del contenido asociado.
//
//   Administrador → puede ver cualquier sesión.
//   Operador      → sólo si la creó él o si identidad-servicio
//                   confirma que el creador es Administrador. En otro
//                   caso, AccesoSesionNoPermitidoExcepcion → 403.
//   Participante  → 403 (lo bloquea la política del controlador).
//   No autenticado→ 401.
//
// El rol del creador NO se guarda en sesiones-servicio: la consulta
// se hace por HTTP contra identidad-servicio mediante el puerto
// IClienteIdentidadUsuarios.
public sealed class ObtenerSesionPorIdManejador
    : IRequestHandler<ObtenerSesionPorIdConsulta, SesionDetalleDto?>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IClienteContenidoJuegos _clienteContenido;
    private readonly IClienteIdentidadUsuarios _clienteIdentidad;

    public ObtenerSesionPorIdManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual,
        IClienteContenidoJuegos clienteContenido,
        IClienteIdentidadUsuarios clienteIdentidad)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _clienteContenido = clienteContenido;
        _clienteIdentidad = clienteIdentidad;
    }

    public async Task<SesionDetalleDto?> Handle(
        ObtenerSesionPorIdConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado)
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Sólo Administrador u Operador pueden consultar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion);
        if (sesion is null) return null;

        var esAdministrador = _usuarioActual.TieneAlgunRol(RolAdministrador);
        if (!esAdministrador)
        {
            var operadorId = _usuarioActual.Id ?? Guid.Empty;
            var esPropia = sesion.CreadaPorUsuarioId == operadorId;

            // Si la sesión es propia, no hace falta preguntar a identidad.
            // En otro caso, identidad-servicio decide si el creador es
            // Administrador.
            var creadaPorAdministrador = !esPropia
                && await _clienteIdentidad.EsAdministradorAsync(
                    sesion.CreadaPorUsuarioId, cancelacion);

            if (!esPropia && !creadaPorAdministrador)
                throw new AccesoSesionNoPermitidoExcepcion(
                    "No tiene permiso para ver esta sesión.");
        }

        var detalle = new SesionDetalleDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            TipoJuego = sesion.TipoJuego.ToString(),
            ContenidoJuegoId = sesion.ContenidoJuegoId,
            Modo = sesion.Modo.ToString(),
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CreadaPorUsuarioId = sesion.CreadaPorUsuarioId,
            FechaCreacion = sesion.FechaCreacion
        };

        if (sesion.TipoJuego == TipoJuego.Trivia)
        {
            detalle.Trivia = await _clienteContenido.ObtenerDetalleTriviaAsync(
                sesion.ContenidoJuegoId, cancelacion);
            if (detalle.Trivia is null)
                throw new ContenidoSesionNoDisponibleExcepcion(
                    "La sesión existe, pero el contenido asociado no está disponible.");
        }
        else
        {
            detalle.BusquedaTesoro = await _clienteContenido.ObtenerDetalleBusquedaTesoroAsync(
                sesion.ContenidoJuegoId, cancelacion);
            if (detalle.BusquedaTesoro is null)
                throw new ContenidoSesionNoDisponibleExcepcion(
                    "La sesión existe, pero el contenido asociado no está disponible.");
        }

        return detalle;
    }
}
