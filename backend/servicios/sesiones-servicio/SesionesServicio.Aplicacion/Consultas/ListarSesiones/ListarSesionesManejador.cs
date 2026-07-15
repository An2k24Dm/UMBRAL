using MediatR;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Consultas.ListarSesiones;

public sealed class ListarSesionesManejador
    : IRequestHandler<ListarSesionesConsulta, List<SesionListadoDto>>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IConsultasSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly FabricaMapeadorListadoSesion _fabricaMapeador;

    public ListarSesionesManejador(
        IConsultasSesiones repositorio,
        IUsuarioActual usuarioActual,
        FabricaMapeadorListadoSesion fabricaMapeador)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _fabricaMapeador = fabricaMapeador;
    }

    public async Task<List<SesionListadoDto>> Handle(
        ListarSesionesConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No tiene permiso para consultar sesiones.");

        Guid? filtroCreador = null;
        if (!_usuarioActual.TieneAlgunRol(RolAdministrador))
            filtroCreador = _usuarioActual.ObtenerId();

        var sesiones = await _repositorio.ListarAsync(consulta.Estado, filtroCreador, cancelacion);

        // El conteo por tipo (participantes/equipos) lo resuelve la estrategia
        // compatible; el manejador no conoce los subtipos de Sesion.
        return sesiones.Select(_fabricaMapeador.Mapear).ToList();
    }
}
