using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.DesactivarParticipante;

public sealed class DesactivarParticipanteComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdParticipante { get; }

    public DesactivarParticipanteComando(Guid idParticipante)
    {
        IdParticipante = idParticipante;
    }
}
