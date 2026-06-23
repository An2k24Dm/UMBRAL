using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.EliminarCuentaParticipante;

// HU11 — eliminación permanente e irreversible de la cuenta del propio
// Participante desde la app móvil.
//
// El cliente NO envía un id de Participante: el controlador extrae el sub
// del token autenticado (IdKeycloak) y lo coloca en el comando. Así un
// Participante jamás puede pedir eliminar a otra cuenta — la identidad sale
// del JWT verificado por el middleware de autenticación.
public sealed class EliminarCuentaParticipanteComando
    : IRequest<EliminarCuentaParticipanteRespuestaDto>
{
    public string IdKeycloak { get; }

    public EliminarCuentaParticipanteComando(string idKeycloak)
    {
        IdKeycloak = idKeycloak;
    }
}
