using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Servicios.Usuarios;

public interface IAutorizadorUsuarioActivo
{
    Task<Usuario> RequerirRolesActivosAsync(
        IEnumerable<RolUsuario> rolesPermitidos, CancellationToken cancelacion);

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
        if (!_usuarioActual.EstaAutenticado ||
            string.IsNullOrWhiteSpace(_usuarioActual.IdKeycloak))
        {
            throw new AccesoNoPermitidoExcepcion(
                "Debe iniciar sesión para realizar esta acción.");
        }

        var usuario = await _repositorioLectura
            .ObtenerPorIdKeycloakAsync(_usuarioActual.IdKeycloak!, cancelacion)
            ?? throw new AccesoNoPermitidoExcepcion(
                "El usuario autenticado no está registrado en UMBRAL.");

        var permitidos = rolesPermitidos as ICollection<RolUsuario>
                         ?? rolesPermitidos.ToList();
        if (!permitidos.Contains(usuario.Rol))
        {
            throw new AccesoNoPermitidoExcepcion(
                "Su rol no le permite realizar esta acción.");
        }

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
