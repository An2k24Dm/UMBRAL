using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed class DesactivarOperadorComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdOperador { get; }

    public DesactivarOperadorComando(Guid idOperador)
    {
        IdOperador = idOperador;
    }
}
