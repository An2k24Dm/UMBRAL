using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;

public sealed class ObtenerPreguntasRespondidasManejador
    : IRequestHandler<ObtenerPreguntasRespondidasConsulta, IReadOnlyList<Guid>>
{
    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IRepositorioRespuestasTrivia _repositorio;

    public ObtenerPreguntasRespondidasManejador(
        IUsuarioActual usuario,
        IRepositorioSesiones repositorioSesiones,
        IRepositorioRespuestasTrivia repositorio)
    {
        _usuario = usuario;
        _repositorioSesiones = repositorioSesiones;
        _repositorio = repositorio;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        ObtenerPreguntasRespondidasConsulta consulta, CancellationToken cancelacion)
    {
        var participanteIdentidadId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(consulta.SesionId, cancelacion);
        var equipoId = ObtenerEquipoId(sesion, participanteIdentidadId);

        return await _repositorio.ObtenerPreguntasRespondidasAsync(
            consulta.SesionId, consulta.EtapaId, participanteIdentidadId, equipoId, cancelacion);
    }

    private static Guid? ObtenerEquipoId(Sesion? sesion, Guid participanteIdentidadId)
    {
        if (sesion is not SesionGrupal grupal) return null;

        foreach (var equipo in grupal.Equipos)
        {
            var p = equipo.Participantes
                .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteIdentidadId);
            if (p is not null) return p.EquipoId;
        }
        return null;
    }
}
