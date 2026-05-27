using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed class DesactivarParticipanteComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdParticipante { get; }

    public DesactivarParticipanteComando(Guid idParticipante)
    {
        IdParticipante = idParticipante;
    }
}
