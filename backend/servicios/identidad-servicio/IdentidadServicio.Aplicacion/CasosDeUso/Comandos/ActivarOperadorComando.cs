using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed class ActivarOperadorComando
    : IRequest<CambiarEstadoUsuarioRespuestaDto>
{
    public Guid IdOperador { get; }

    public ActivarOperadorComando(Guid idOperador)
    {
        IdOperador = idOperador;
    }
}
