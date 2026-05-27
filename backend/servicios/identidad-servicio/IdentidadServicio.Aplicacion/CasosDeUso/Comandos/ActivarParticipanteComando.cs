using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed class ActivarParticipanteComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdParticipante { get; }

    public ActivarParticipanteComando(Guid idParticipante)
    {
        IdParticipante = idParticipante;
    }
}
