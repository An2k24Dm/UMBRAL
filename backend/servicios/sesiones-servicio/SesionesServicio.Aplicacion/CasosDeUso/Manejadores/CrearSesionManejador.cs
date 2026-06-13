using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class CrearSesionManejador
    : IRequestHandler<CrearSesionComando, CrearSesionRespuestaDto>
{
    private const string RolOperador = "Operador";
    private const string EstadoMisionActiva = "Activa";

    private readonly IValidador<CrearSesionComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IClienteJuegosMisiones _clienteMisiones;
    private readonly IGeneradorCodigoAcceso _generadorCodigo;
    private readonly IProveedorFechaHora _reloj;
    private readonly IFabricaSesion _fabricaSesion;

    public CrearSesionManejador(
        IValidador<CrearSesionComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IClienteJuegosMisiones clienteMisiones,
        IGeneradorCodigoAcceso generadorCodigo,
        IProveedorFechaHora reloj,
        IFabricaSesion fabricaSesion)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _clienteMisiones = clienteMisiones;
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

        await ValidarMisionesAsync(comando.Datos.MisionesIds, cancelacion);

        var operadorId = _usuarioActual.ObtenerId()
            ?? throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No se pudo determinar la identidad del operador.");

        var codigoAcceso = _generadorCodigo.Generar();

        // La fábrica selecciona el creador compatible con el modo; el
        // manejador no conoce SesionIndividual ni SesionGrupal.
        var sesion = _fabricaSesion.Crear(
            comando.Datos.Modo,
            comando.Datos.Nombre, comando.Datos.Descripcion,
            fechaProgramada, codigoAcceso, operadorId, ahoraUtc);

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

    private async Task ValidarMisionesAsync(
        IReadOnlyList<Guid> misionesIds, CancellationToken cancelacion)
    {
        foreach (var misionId in misionesIds)
        {
            var mision = await _clienteMisiones.ObtenerMisionAsync(misionId, cancelacion);
            if (mision is null)
                throw new MisionNoEncontradaExcepcion(
                    $"La misión {misionId} no existe.");
            if (!string.Equals(mision.Estado, EstadoMisionActiva, StringComparison.OrdinalIgnoreCase))
                throw new MisionNoActivaExcepcion(
                    $"La misión '{mision.Nombre}' no está activa.");
            if (mision.TotalEtapas <= 0)
                throw new MisionSinEtapasExcepcion(
                    $"La misión '{mision.Nombre}' no tiene etapas.");
        }
    }
}
