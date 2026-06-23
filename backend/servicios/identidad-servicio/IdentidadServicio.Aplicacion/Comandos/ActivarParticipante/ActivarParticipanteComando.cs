using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ActivarParticipante;

public sealed class ActivarParticipanteComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdParticipante { get; }

    public ActivarParticipanteComando(Guid idParticipante)
    {
        IdParticipante = idParticipante;
    }
}
