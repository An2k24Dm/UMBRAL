using MediatR;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ListarEquiposSesion;

public sealed class ListarEquiposSesionManejador
    : IRequestHandler<ListarEquiposSesionConsulta, IReadOnlyList<EquipoSesionListadoDto>>
{
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;

    public ListarEquiposSesionManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<EquipoSesionListadoDto>> Handle(
        ListarEquiposSesionConsulta consulta, CancellationToken cancelacion)
    {
        var (sesion, usuarioId) = await AccesoConsultaEquipos.ResolverSesionAutorizadaAsync(
            consulta.SesionId, _repositorio, _usuarioActual, cancelacion);

        return sesion.Equipos
            .Select(equipo => new EquipoSesionListadoDto
            {
                Id = equipo.Id,
                SesionId = equipo.SesionId,
                Nombre = equipo.Nombre.Valor,
                Tipo = equipo.Tipo.ToString(),
                Puntaje = equipo.Puntaje,
                CantidadParticipantes = equipo.Participantes.Count,
                CapacidadMaxima = equipo.CapacidadMaxima,
                EstaLleno = equipo.EstaLleno(),
                FechaCreacion = equipo.FechaCreacion,
                EsMiEquipo = AccesoConsultaEquipos.EsMiEquipo(equipo, usuarioId),
                SoyLider = AccesoConsultaEquipos.SoyLider(equipo, usuarioId)
            })
            .OrderBy(e => e.Nombre, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
