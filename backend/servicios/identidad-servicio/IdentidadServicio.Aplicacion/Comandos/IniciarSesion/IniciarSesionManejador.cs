using IdentidadServicio.Aplicacion.Enums;
using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.Comandos.IniciarSesion;

public sealed class IniciarSesionManejador
    : IRequestHandler<IniciarSesionComando, ResultadoInicioSesionDto>
{
    private readonly IProveedorIdentidad _proveedor;
    private readonly IRepositorioUsuariosLectura _repositorioLectura;
    private readonly IRepositorioControlContrasenaTemporal _controlContrasena;
    private readonly ILogger<IniciarSesionManejador> _registro;

    public IniciarSesionManejador(
        IProveedorIdentidad proveedor,
        IRepositorioUsuariosLectura repositorioLectura,
        IRepositorioControlContrasenaTemporal controlContrasena,
        ILogger<IniciarSesionManejador> registro)
    {
        _proveedor = proveedor;
        _repositorioLectura = repositorioLectura;
        _controlContrasena = controlContrasena;
        _registro = registro;
    }

    public async Task<ResultadoInicioSesionDto> Handle(
        IniciarSesionComando comando, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(comando.NombreUsuario) ||
            string.IsNullOrWhiteSpace(comando.Contrasena))
        {
            throw new DatosUsuarioInvalidosExcepcion("Debe indicar nombre de usuario y contraseña.");
        }

        var autenticacion = await _proveedor.IniciarSesionAsync(
                                comando.NombreUsuario.Trim().ToLowerInvariant(),
                                comando.Contrasena,
                                cancelacion)
                            ?? throw new DatosUsuarioInvalidosExcepcion("Credenciales inválidas.");

        var usuario = await _repositorioLectura.ObtenerPorIdKeycloakAsync(autenticacion.IdKeycloak, cancelacion)
                      ?? throw new DatosUsuarioInvalidosExcepcion("El usuario no está registrado en UMBRAL.");

        usuario.ValidarPuedeIniciarSesion();

        ValidarOrigenPermitido(comando.Origen, usuario.Rol);

        var requiereCambio = await _controlContrasena.ObtenerDebeCambiarPorIdKeycloakAsync(
            autenticacion.IdKeycloak, cancelacion);

        _registro.LogInformation(
            "Inicio de sesión exitoso para {NombreUsuario} (rol {Rol}, origen {Origen}, " +
            "requiereCambioContrasena={RequiereCambio}).",
            usuario.NombreUsuario.Valor, usuario.Rol, comando.Origen, requiereCambio);

        return new ResultadoInicioSesionDto
        {
            TokenAcceso = autenticacion.TokenAcceso,
            TokenRefresco = autenticacion.TokenRefresco,
            ExpiraEn = autenticacion.ExpiraEnSegundos,
            TipoToken = autenticacion.TipoToken,
            Usuario = DtoMapeador.AUsuarioAutenticado(usuario),
            RutaRedireccion = DtoMapeador.ResolverRutaPorRol(usuario.Rol),
            RequiereCambioContrasena = requiereCambio
        };
    }

    private static void ValidarOrigenPermitido(OrigenInicioSesion origen, RolUsuario rol)
    {
        if (origen == OrigenInicioSesion.Web && rol == RolUsuario.Participante)
        {
            throw new AccesoNoPermitidoExcepcion(
                "Los participantes solo pueden iniciar sesión desde la aplicación móvil.");
        }

        if (origen == OrigenInicioSesion.Movil && rol != RolUsuario.Participante)
        {
            throw new AccesoNoPermitidoExcepcion(
                "Solo los participantes pueden iniciar sesión desde la aplicación móvil.");
        }
    }
}
