using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ModificarParticipante;

public sealed class ModificarParticipanteComando
    : IRequest<ModificarParticipanteRespuestaDto>
{
    public string IdKeycloak { get; }
    public ModificarParticipanteSolicitudDto Datos { get; }

    public Guid IdParticipanteActual { get; set; }

    public ModificarParticipanteComando(string idKeycloak, ModificarParticipanteSolicitudDto datos)
    {
        IdKeycloak = idKeycloak;
        Datos = datos;
    }
}
