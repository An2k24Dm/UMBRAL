using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Enums;
using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class IniciarSesionManejador
    : IRequestHandler<IniciarSesionComando, ResultadoInicioSesionDto>
{
    private readonly IProveedorIdentidad _proveedor;
    private readonly IRepositorioIdentidad _repositorio;
    private readonly ILogger<IniciarSesionManejador> _registro;

    public IniciarSesionManejador(
        IProveedorIdentidad proveedor,
        IRepositorioIdentidad repositorio,
        ILogger<IniciarSesionManejador> registro)
    {
        _proveedor = proveedor;
        _repositorio = repositorio;
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

        // 1) Autenticar contra Keycloak.
        var autenticacion = await _proveedor.IniciarSesionAsync(
                                comando.NombreUsuario.Trim().ToLowerInvariant(),
                                comando.Contrasena,
                                cancelacion)
                            ?? throw new DatosUsuarioInvalidosExcepcion("Credenciales inválidas.");

        // 2) Buscar el usuario interno por IdKeycloak.
        var usuario = await _repositorio.ObtenerPorIdKeycloakAsync(autenticacion.IdKeycloak, cancelacion)
                      ?? throw new DatosUsuarioInvalidosExcepcion("El usuario no está registrado en UMBRAL.");

        // 3) Reglas de dominio (cuenta desactivada / rol inválido).
        usuario.ValidarPuedeIniciarSesion();

        // 4) Regla de acceso por aplicación.
        //    Web   → Administrador, Operador
        //    Movil → Participante
        ValidarOrigenPermitido(comando.Origen, usuario.Rol);

        _registro.LogInformation(
            "Inicio de sesión exitoso para {NombreUsuario} (rol {Rol}, origen {Origen}).",
            usuario.NombreUsuario.Valor, usuario.Rol, comando.Origen);

        return new ResultadoInicioSesionDto
        {
            TokenAcceso = autenticacion.TokenAcceso,
            TokenRefresco = autenticacion.TokenRefresco,
            ExpiraEn = autenticacion.ExpiraEnSegundos,
            TipoToken = autenticacion.TipoToken,
            Usuario = DtoMapeador.AUsuarioAutenticado(usuario),
            RutaRedireccion = DtoMapeador.ResolverRutaPorRol(usuario.Rol)
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
