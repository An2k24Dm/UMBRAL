using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.EliminarOperador;

public sealed class EliminarOperadorComando
    : IRequest<EliminarOperadorRespuestaDto>
{
    public Guid IdOperador { get; }

    public EliminarOperadorComando(Guid idOperador)
    {
        IdOperador = idOperador;
    }
}
