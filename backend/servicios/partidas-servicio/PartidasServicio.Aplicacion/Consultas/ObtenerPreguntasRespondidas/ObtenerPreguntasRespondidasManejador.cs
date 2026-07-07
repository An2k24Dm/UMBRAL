using MediatR;
using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;

public sealed class ObtenerPreguntasRespondidasManejador
    : IRequestHandler<ObtenerPreguntasRespondidasConsulta, IReadOnlyList<Guid>>
{
    private readonly IConsultasPartidas _consultas;
    private readonly IClienteSesiones _clienteSesiones;
    private readonly IUsuarioActual _usuarioActual;

    public ObtenerPreguntasRespondidasManejador(
        IConsultasPartidas consultas,
        IClienteSesiones clienteSesiones,
        IUsuarioActual usuarioActual)
    {
        _consultas = consultas;
        _clienteSesiones = clienteSesiones;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        ObtenerPreguntasRespondidasConsulta consulta, CancellationToken cancelacion)
    {
        var participanteId = _usuarioActual.ObtenerId();

        Guid? equipoId = null;
        if (participanteId.HasValue)
        {
            var info = await _clienteSesiones.ObtenerInfoPartidaAsync(consulta.SesionId, cancelacion);
            equipoId = info?.EquipoId;
        }

        return await _consultas.ObtenerPreguntasRespondidasAsync(
            consulta.SesionId,
            consulta.MisionId,
            consulta.EtapaId,
            equipoId,
            equipoId is null ? participanteId : null,
            cancelacion);
    }
}
