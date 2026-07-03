using SesionesServicio.Aplicacion.Validaciones;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteEquipo;

// HU45 — Validación de formato rápida; las reglas de negocio (estado de la
// sesión, líder, reasignación) las protege el dominio.
public sealed class ExpulsarParticipanteEquipoValidador
    : ValidadorBase<ExpulsarParticipanteEquipoComando>
{
    protected override void ValidarSolicitud(
        ExpulsarParticipanteEquipoComando comando, ResultadoValidacion resultado)
    {
        if (comando.SesionId == Guid.Empty)
            resultado.Agregar("sesionId", "El identificador de la sesión es obligatorio.");

        if (comando.EquipoId == Guid.Empty)
            resultado.Agregar("equipoId", "El identificador del equipo es obligatorio.");

        if (comando.ParticipanteSesionId == Guid.Empty)
            resultado.Agregar(
                "participanteSesionId",
                "El identificador del participante es obligatorio.");
    }
}
