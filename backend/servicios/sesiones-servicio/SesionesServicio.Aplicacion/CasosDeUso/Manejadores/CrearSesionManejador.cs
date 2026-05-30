using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

// HU33 — Crear una sesión Programada a partir de un contenido Activo.
//
// El manejador orquesta las reglas que no caben en el ValidadorCrearSesion:
//  * Comprueba que el usuario actual esté autenticado y tenga rol
//    Administrador u Operador (rol leído desde claims del JWT).
//  * Consulta juegos-servicio (vía IClienteContenidoJuegos) para
//    confirmar que el contenido existe y está Activo.
//  * Construye el agregado Sesion y lo persiste.
//
// El handler no toca EF Core ni HttpClient directamente; depende sólo de
// los puertos de Aplicación. Esto mantiene Aplicación libre de
// dependencias de infraestructura, conforme a la arquitectura hexagonal.
public sealed class CrearSesionManejador
    : IRequestHandler<CrearSesionComando, CrearSesionRespuestaDto>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IValidador<CrearSesionComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IClienteContenidoJuegos _clienteContenido;
    private readonly IProveedorFechaHora _reloj;

    public CrearSesionManejador(
        IValidador<CrearSesionComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IClienteContenidoJuegos clienteContenido,
        IProveedorFechaHora reloj)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _clienteContenido = clienteContenido;
        _reloj = reloj;
    }

    public async Task<CrearSesionRespuestaDto> Handle(
        CrearSesionComando comando, CancellationToken cancelacion)
    {
        // 1. Validación de entrada (forma del DTO).
        _validador.Validar(comando).LanzarSiHayErrores();

        // 2. Autorización: usuario autenticado con rol Administrador u
        //    Operador. Los participantes y anónimos quedan fuera.
        if (!_usuarioActual.EstaAutenticado)
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para crear una sesión.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Sólo Administrador u Operador pueden crear sesiones.");

        // 3. Reinterpretar enums ya validados.
        var tipoJuego = Enum.Parse<TipoJuego>(comando.Datos.TipoJuego, ignoreCase: true);
        var modo = Enum.Parse<ModoSesion>(comando.Datos.Modo, ignoreCase: true);

        // 4. Verificar contenido contra juegos-servicio.
        var contenido = await _clienteContenido.ObtenerContenidoAsync(
            tipoJuego, comando.Datos.ContenidoJuegoId, cancelacion);

        if (contenido is null)
            throw new ContenidoJuegoNoEncontradoExcepcion(
                "El contenido seleccionado no existe.");

        if (!contenido.EstaActivo)
            throw new ContenidoJuegoNoActivoExcepcion(
                "No se puede crear una sesión desde un contenido inactivo.");

        // 5. Construir agregado Sesion (estado inicial Programada).
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var creadorId = _usuarioActual.Id ?? Guid.Empty;

        var sesion = Sesion.Crear(
            comando.Datos.Nombre,
            tipoJuego,
            comando.Datos.ContenidoJuegoId,
            modo,
            comando.Datos.FechaProgramada,
            creadorId,
            ahoraUtc);

        await _repositorio.AgregarAsync(sesion, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return new CrearSesionRespuestaDto
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
    }
}
