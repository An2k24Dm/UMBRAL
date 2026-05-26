using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

// HU10 — edición del propio perfil del Participante desde la app móvil.
//
// El cliente NO envía un id de Participante: el controlador extrae el sub
// del token autenticado (IdKeycloak) y lo pone en el comando. Así un
// Participante jamás puede pedir editar a otra cuenta — la identidad sale
// del JWT verificado por el middleware de autenticación.
//
// IdParticipanteActual es un campo interno que el manejador rellena tras
// resolver el agregado por IdKeycloak. Lo usa el validador de unicidad para
// excluir al propio Participante en las consultas de duplicados. No forma
// parte del contrato de entrada (no se acepta del cliente).
public sealed class ModificarParticipanteComando
    : IRequest<ModificarParticipanteRespuestaDto>
{
    public string IdKeycloak { get; }
    public ModificarParticipanteSolicitudDto Datos { get; }

    // Se setea internamente desde el manejador una vez resuelto el agregado.
    public Guid IdParticipanteActual { get; set; }

    public ModificarParticipanteComando(string idKeycloak, ModificarParticipanteSolicitudDto datos)
    {
        IdKeycloak = idKeycloak;
        Datos = datos;
    }
}
