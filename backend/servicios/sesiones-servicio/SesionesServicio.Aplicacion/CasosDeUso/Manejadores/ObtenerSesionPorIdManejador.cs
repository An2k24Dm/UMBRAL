using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerSesionPorIdManejador
    : IRequestHandler<ObtenerSesionPorIdConsulta, SesionDetalleDto?>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly FabricaMapeadorDetalleSesion _fabricaMapeador;

    public ObtenerSesionPorIdManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual,
        FabricaMapeadorDetalleSesion fabricaMapeador)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _fabricaMapeador = fabricaMapeador;
    }

    public async Task<SesionDetalleDto?> Handle(
        ObtenerSesionPorIdConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No tiene permiso para consultar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion);
        if (sesion is null) return null;

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador))
        {
            var operadorId = _usuarioActual.ObtenerId() ?? Guid.Empty;
            if (sesion.OperadorCreadorId != operadorId)
                throw new AccesoSesionNoPermitidoExcepcion(
                    "No tiene permiso para ver esta sesión.");
        }

        // El mapeo del detalle (incluida la parte específica del tipo de
        // sesión) lo resuelve la estrategia compatible. El manejador no
        // conoce SesionIndividual ni SesionGrupal.
        return _fabricaMapeador.Mapear(sesion);
    }
}
