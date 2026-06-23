using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Fabricas;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Aplicacion.Comandos.CrearSesion;

public sealed class CrearSesionManejador
    : IRequestHandler<CrearSesionComando, CrearSesionRespuestaDto>
{
    private const string RolOperador = "Operador";

    private readonly IValidador<CrearSesionComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IValidadorMisionesSesion _validadorMisiones;
    private readonly IGeneradorCodigoAcceso _generadorCodigo;
    private readonly IProveedorFechaHora _reloj;
    private readonly IFabricaSesion _fabricaSesion;

    public CrearSesionManejador(
        IValidador<CrearSesionComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IValidadorMisionesSesion validadorMisiones,
        IGeneradorCodigoAcceso generadorCodigo,
        IProveedorFechaHora reloj,
        IFabricaSesion fabricaSesion)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _validadorMisiones = validadorMisiones;
        _generadorCodigo = generadorCodigo;
        _reloj = reloj;
        _fabricaSesion = fabricaSesion;
    }

    public async Task<CrearSesionRespuestaDto> Handle(
        CrearSesionComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para crear una sesión.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Solo un Operador puede crear sesiones.");

        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var fechaProgramada = comando.Datos.FechaProgramada.Kind == DateTimeKind.Utc
            ? comando.Datos.FechaProgramada
            : DateTime.SpecifyKind(comando.Datos.FechaProgramada, DateTimeKind.Utc);
        PoliticaProgramacionSesion.ValidarFechaProgramada(fechaProgramada, ahoraUtc);

        await _validadorMisiones.ValidarAsync(comando.Datos.MisionesIds, cancelacion);

        var operadorId = _usuarioActual.ObtenerId()
            ?? throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No se pudo determinar la identidad del operador.");

        var codigoAcceso = _generadorCodigo.Generar();

        var datosCreacion = new DatosCreacionSesion(
            comando.Datos.Modo,
            comando.Datos.Nombre,
            comando.Datos.Descripcion,
            fechaProgramada,
            codigoAcceso,
            operadorId,
            ahoraUtc,
            comando.Datos.MaximoParticipantes,
            comando.Datos.MaximoEquipos,
            comando.Datos.MaximoParticipantesPorEquipo);

        var sesion = _fabricaSesion.Crear(datosCreacion);

        sesion.AsignarMisiones(comando.Datos.MisionesIds);

        await _repositorio.AgregarAsync(sesion, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return new CrearSesionRespuestaDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Modo = sesion.TipoSesion,
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso,
            OperadorCreadorId = sesion.OperadorCreadorId,
            FechaCreacion = sesion.FechaCreacion,
            MisionesIds = sesion.Misiones.OrderBy(m => m.Orden).Select(m => m.MisionId).ToList()
        };
    }
}
