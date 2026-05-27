using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Servicios.Usuarios;

// HU12 — servicio reutilizable de "autorización de usuario actual con
// validación de Estado Activo". Lo usan todos los manejadores que necesitan
// asegurarse de que el portador del token:
//  * está autenticado;
//  * existe como usuario interno de UMBRAL;
//  * tiene uno de los roles permitidos;
//  * está Activo en la base de datos (no solo en Keycloak).
//
// La última condición es la novedad de HU12: aunque Keycloak siga
// devolviendo tokens válidos, un Operador o Participante recientemente
// desactivado debe perder acceso a las acciones protegidas. Esto evita que
// un token emitido antes de la desactivación siga siendo utilizable hasta
// que expire.
//
// Excepciones:
//  * AccesoNoPermitidoExcepcion → 403 (anónimo no aplica aquí: el
//    middleware de autenticación ya emite 401 antes de que el manejador
//    corra; pero defendemos por si el endpoint estuviera mal anotado).
//  * CuentaDesactivadaExcepcion → 403 (mismo código HTTP en
//    ManejadorErroresMiddleware).
public interface IAutorizadorUsuarioActivo
{
    // Verifica que el usuario actual exista, tenga uno de los roles
    // permitidos y esté Activo. Devuelve el agregado Usuario resuelto
    // (útil para acciones que también necesitan su id interno).
    Task<Usuario> RequerirRolesActivosAsync(
        IEnumerable<RolUsuario> rolesPermitidos, CancellationToken cancelacion);

    // Atajo: un solo rol exigido.
    Task<Usuario> RequerirRolActivoAsync(
        RolUsuario rolRequerido, CancellationToken cancelacion);
}

public sealed class AutorizadorUsuarioActivo : IAutorizadorUsuarioActivo
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRepositorioUsuariosLectura _repositorioLectura;

    public AutorizadorUsuarioActivo(
        IUsuarioActual usuarioActual,
        IRepositorioUsuariosLectura repositorioLectura)
    {
        _usuarioActual = usuarioActual;
        _repositorioLectura = repositorioLectura;
    }

    public async Task<Usuario> RequerirRolesActivosAsync(
        IEnumerable<RolUsuario> rolesPermitidos, CancellationToken cancelacion)
    {
        // 1) Autenticación: sin sub no podemos resolver al usuario.
        if (!_usuarioActual.EstaAutenticado ||
            string.IsNullOrWhiteSpace(_usuarioActual.IdKeycloak))
        {
            throw new AccesoNoPermitidoExcepcion(
                "Debe iniciar sesión para realizar esta acción.");
        }

        // 2) Resolución del agregado por el sub del token.
        var usuario = await _repositorioLectura
            .ObtenerPorIdKeycloakAsync(_usuarioActual.IdKeycloak!, cancelacion)
            ?? throw new AccesoNoPermitidoExcepcion(
                "El usuario autenticado no está registrado en UMBRAL.");

        // 3) Rol permitido.
        var permitidos = rolesPermitidos as ICollection<RolUsuario>
                         ?? rolesPermitidos.ToList();
        if (!permitidos.Contains(usuario.Rol))
        {
            throw new AccesoNoPermitidoExcepcion(
                "Su rol no le permite realizar esta acción.");
        }

        // 4) Estado Activo. Reusamos la regla del dominio para mantener un
        //    único punto de verdad.
        if (usuario.Estado != EstadoUsuario.Activo)
        {
            throw new CuentaDesactivadaExcepcion();
        }

        return usuario;
    }

    public Task<Usuario> RequerirRolActivoAsync(
        RolUsuario rolRequerido, CancellationToken cancelacion)
        => RequerirRolesActivosAsync(new[] { rolRequerido }, cancelacion);
}
