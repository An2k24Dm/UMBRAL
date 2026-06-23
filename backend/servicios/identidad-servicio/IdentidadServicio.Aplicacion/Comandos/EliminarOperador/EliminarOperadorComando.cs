using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.EliminarOperador;

// HU13 — eliminación permanente e irreversible de la cuenta de un Operador
// por parte de un Administrador desde el panel web.
//
// A diferencia de HU11 (Participante elimina su propia cuenta), el id viaja
// por la ruta porque el Administrador selecciona un Operador concreto desde
// el listado. El backend revalida que el id corresponda realmente a un
// Operador para evitar que se elimine un Administrador o un Participante.
public sealed class EliminarOperadorComando
    : IRequest<EliminarOperadorRespuestaDto>
{
    public Guid IdOperador { get; }

    public EliminarOperadorComando(Guid idOperador)
    {
        IdOperador = idOperador;
    }
}
