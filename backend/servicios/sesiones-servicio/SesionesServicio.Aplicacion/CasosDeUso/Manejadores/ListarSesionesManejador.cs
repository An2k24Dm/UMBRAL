using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ListarSesionesManejador
    : IRequestHandler<ListarSesionesConsulta, List<SesionListadoDto>>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;

    public ListarSesionesManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
    }

    public async Task<List<SesionListadoDto>> Handle(
        ListarSesionesConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado)
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No tiene permiso para consultar sesiones.");

        Guid? filtroCreador = null;
        if (!_usuarioActual.TieneAlgunRol(RolAdministrador))
            filtroCreador = _usuarioActual.Id;

        var sesiones = await _repositorio.ListarAsync(consulta.Estado, filtroCreador, cancelacion);

        return sesiones.Select(s => new SesionListadoDto
        {
            Id = s.Id,
            Nombre = s.Nombre,
            Descripcion = s.Descripcion,
            Modo = s.TipoSesion,
            Estado = s.Estado.ToString(),
            FechaProgramada = s.FechaProgramada,
            CodigoAcceso = s.CodigoAcceso,
            OperadorCreadorId = s.OperadorCreadorId,
            FechaCreacion = s.FechaCreacion,
            CantidadMisiones = s.Misiones.Count,
            CantidadParticipantes = s is SesionIndividual ind ? ind.Participantes.Count : 0,
            CantidadEquipos = s is SesionGrupal grp ? grp.Equipos.Count : 0
        }).ToList();
    }
}
