using SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionParticipante;

namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class ValidadorAplicarPenalizacionParticipante
    : ValidadorBase<AplicarPenalizacionParticipanteComando>
{
    protected override void ValidarSolicitud(
        AplicarPenalizacionParticipanteComando comando,
        ResultadoValidacion resultado)
    {
        if (comando.SesionId == Guid.Empty)
            resultado.Agregar("sesionId", "El identificador de la sesión es obligatorio.");

        if (comando.ParticipanteSesionId == Guid.Empty)
            resultado.Agregar(
                "participanteSesionId",
                "El identificador del participante es obligatorio.");

        ValidacionesPenalizacion.ValidarPuntos(comando.Puntos, resultado);
        ValidacionesPenalizacion.ValidarMotivo(comando.Motivo, resultado);
    }
}
