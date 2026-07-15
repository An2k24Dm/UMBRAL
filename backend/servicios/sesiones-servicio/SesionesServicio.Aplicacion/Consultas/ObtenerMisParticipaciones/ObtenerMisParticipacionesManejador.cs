using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMisParticipaciones;

public sealed class ObtenerMisParticipacionesManejador
    : IRequestHandler<ObtenerMisParticipacionesConsulta, IReadOnlyList<MiParticipacionDto>>
{
    private readonly IConsultasSesiones _consultas;
    private readonly IUsuarioActual _usuarioActual;

    public ObtenerMisParticipacionesManejador(
        IConsultasSesiones consultas,
        IUsuarioActual usuarioActual)
    {
        _consultas = consultas;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<MiParticipacionDto>> Handle(
        ObtenerMisParticipacionesConsulta consulta, CancellationToken cancelacion)
    {
        var participanteId = _usuarioActual.ObtenerId();
        if (participanteId is not Guid pid || pid == Guid.Empty)
            return Array.Empty<MiParticipacionDto>();

        var proyecciones = await _consultas.ListarParticipacionesFinalizadasAsync(
            pid, consulta.Limite, cancelacion);

        return proyecciones
            .Select(p => new MiParticipacionDto
            {
                SesionId = p.SesionId,
                NombreSesion = p.NombreSesion,
                Modo = p.Modo,
                FechaInicioUtc = p.FechaInicioUtc,
                FechaFinalizacionUtc = p.FechaFinalizacionUtc,
                PuntajeObtenido = p.Puntaje
            })
            .ToList()
            .AsReadOnly();
    }
}
