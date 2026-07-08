using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;

public sealed class ObtenerPreguntasRespondidasManejador
    : IRequestHandler<ObtenerPreguntasRespondidasConsulta, IReadOnlyList<Guid>>
{
    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioRespuestasTrivia _repositorio;

    public ObtenerPreguntasRespondidasManejador(
        IUsuarioActual usuario,
        IRepositorioRespuestasTrivia repositorio)
    {
        _usuario = usuario;
        _repositorio = repositorio;
    }

    public Task<IReadOnlyList<Guid>> Handle(
        ObtenerPreguntasRespondidasConsulta consulta, CancellationToken cancelacion)
    {
        var participanteIdentidadId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        return _repositorio.ObtenerPreguntasRespondidasAsync(
            consulta.SesionId, consulta.EtapaId, participanteIdentidadId, cancelacion);
    }
}
