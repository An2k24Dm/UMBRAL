using PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;

namespace PartidasServicio.Aplicacion.Validaciones;

public sealed class ValidadorEnviarRespuestaTrivia : ValidadorBase<EnviarRespuestaTriviaComando>
{
    protected override void ValidarSolicitud(
        EnviarRespuestaTriviaComando solicitud, ResultadoValidacion resultado)
    {
        if (solicitud.SesionId == Guid.Empty)
            resultado.Agregar("sesionId", "El identificador de la sesión es obligatorio.");
        if (solicitud.MisionId == Guid.Empty)
            resultado.Agregar("misionId", "El identificador de la misión es obligatorio.");
        if (solicitud.EtapaId == Guid.Empty)
            resultado.Agregar("etapaId", "El identificador de la etapa es obligatorio.");
        if (solicitud.Dto.PreguntaId == Guid.Empty)
            resultado.Agregar("preguntaId", "El identificador de la pregunta es obligatorio.");
        if (solicitud.Dto.OpcionSeleccionadaId == Guid.Empty)
            resultado.Agregar("opcionSeleccionadaId", "El identificador de la opción es obligatorio.");
        if (solicitud.Dto.TiempoTardadoMs < 0)
            resultado.Agregar("tiempoTardadoMs", "El tiempo tardado no puede ser negativo.");
    }
}
