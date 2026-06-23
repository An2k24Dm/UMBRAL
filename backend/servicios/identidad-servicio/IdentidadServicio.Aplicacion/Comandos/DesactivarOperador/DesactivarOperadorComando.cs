using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.DesactivarOperador;

public sealed class DesactivarOperadorComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdOperador { get; }

    public DesactivarOperadorComando(Guid idOperador)
    {
        IdOperador = idOperador;
    }
}
