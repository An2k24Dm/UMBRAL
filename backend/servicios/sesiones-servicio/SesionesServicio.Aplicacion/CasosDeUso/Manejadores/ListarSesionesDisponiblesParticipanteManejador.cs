using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ListarSesionesDisponiblesParticipanteManejador
    : IRequestHandler<ListarSesionesDisponiblesParticipanteConsulta,
        List<SesionDisponibleMovilDto>>
{
    private readonly IRepositorioSesiones _repositorio;

    public ListarSesionesDisponiblesParticipanteManejador(
        IRepositorioSesiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<SesionDisponibleMovilDto>> Handle(
        ListarSesionesDisponiblesParticipanteConsulta consulta,
        CancellationToken cancelacion)
    {
        var modoNormalizado = consulta.Modo;
        if (string.IsNullOrWhiteSpace(modoNormalizado)
            || string.Equals(modoNormalizado, "Todas", StringComparison.OrdinalIgnoreCase))
        {
            modoNormalizado = null;
        }

        var sesiones = await _repositorio.ListarDisponiblesParaParticipanteAsync(
            consulta.Busqueda, modoNormalizado, cancelacion);

        return sesiones.Select(MapearADto).ToList();
    }

    private static SesionDisponibleMovilDto MapearADto(Sesion sesion)
    {
        var dto = new SesionDisponibleMovilDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Modo = sesion.TipoSesion,
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CantidadMisiones = sesion.Misiones.Count
        };

        // Las capacidades dependen del subtipo concreto. Las exponemos
        // solo cuando aplican para no confundir a la UI con campos
        // null que en realidad no corresponden al modo.
        if (sesion is SesionIndividual individual)
        {
            dto.CantidadParticipantesActuales = individual.Participantes.Count;
            dto.CapacidadMaximaParticipantes =
                PoliticaCapacidadSesion.MaximoParticipantesIndividual;
        }
        else if (sesion is SesionGrupal grupal)
        {
            dto.CantidadEquiposActuales = grupal.Equipos.Count;
            dto.CapacidadMaximaEquipos =
                PoliticaCapacidadSesion.MaximoEquiposPorSesion;
        }

        return dto;
    }
}
