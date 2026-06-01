using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

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
//
// HU34 — La sesión NO guarda el rol del creador. La regla de visibilidad
// del listado/detalle se resuelve en línea consultando identidad-servicio
// (ver ListarSesionesManejador y ObtenerSesionPorIdManejador).
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

        // 2. Política de dominio: la fecha programada debe ser futura.
        //    Aplicación obtiene "ahora" del puerto IProveedorFechaHora y
        //    se lo pasa a la política. La política vive en Dominio y no
        //    conoce relojes ni puertos; lanza SesionInvalidaExcepcion
        //    si la regla no se cumple → 400 a través del middleware.
        //    Se interpreta como UTC cuando la fecha llega sin Kind para
        //    no depender de la zona horaria del proceso del servidor.
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var fechaProgramada = comando.Datos.FechaProgramada.Kind == DateTimeKind.Utc
            ? comando.Datos.FechaProgramada
            : DateTime.SpecifyKind(comando.Datos.FechaProgramada, DateTimeKind.Utc);
        PoliticaProgramacionSesion.ValidarFechaProgramada(fechaProgramada, ahoraUtc);

        // 3. Autorización: usuario autenticado con rol Administrador u
        //    Operador. Los participantes y anónimos quedan fuera.
        if (!_usuarioActual.EstaAutenticado)
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para crear una sesión.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Sólo Administrador u Operador pueden crear sesiones.");

        // 4. Reinterpretar enums ya validados.
        var tipoJuego = Enum.Parse<TipoJuego>(comando.Datos.TipoJuego, ignoreCase: true);
        var modo = Enum.Parse<ModoSesion>(comando.Datos.Modo, ignoreCase: true);

        // 5. Verificar contenido contra juegos-servicio.
        var contenido = await _clienteContenido.ObtenerContenidoAsync(
            tipoJuego, comando.Datos.ContenidoJuegoId, cancelacion);

        if (contenido is null)
            throw new ContenidoJuegoNoEncontradoExcepcion(
                "El contenido seleccionado no existe.");

        if (!contenido.EstaActivo)
            throw new ContenidoJuegoNoActivoExcepcion(
                "No se puede crear una sesión desde un contenido inactivo.");

        // 6. Construir agregado Sesion (estado inicial Programada).
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
