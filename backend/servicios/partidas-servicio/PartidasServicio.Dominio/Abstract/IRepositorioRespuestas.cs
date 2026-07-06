using PartidasServicio.Dominio.Entidades;

namespace PartidasServicio.Dominio.Abstract;

public interface IRepositorioRespuestas
{
    Task AgregarAsync(RespuestaTrivia respuesta, CancellationToken cancelacion);
    Task<bool> YaRespondioEquipoAsync(Guid sesionId, Guid preguntaId, Guid equipoId, CancellationToken cancelacion);
    Task<bool> YaRespondioParticipanteAsync(Guid sesionId, Guid preguntaId, Guid participanteId, CancellationToken cancelacion);
}
