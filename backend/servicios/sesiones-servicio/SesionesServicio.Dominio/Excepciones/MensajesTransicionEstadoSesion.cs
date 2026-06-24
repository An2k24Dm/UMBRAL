using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Excepciones;

internal static class MensajesTransicionEstadoSesion
{
    public const string EstadoNoValido = "El estado de la sesión no es válido.";

    private static readonly Dictionary<(EstadoSesion Estado, string Accion), string> Mensajes = new()
    {
        // Programada
        [(EstadoSesion.Programada, "Cancelar")] =
            "Una sesión programada no se cancela; debe eliminarse.",
        [(EstadoSesion.Programada, "Iniciar")] =
            "Una sesión Programada no puede iniciarse directamente. Debe pasar primero a EnPreparacion.",
        [(EstadoSesion.Programada, "Pausar")] = "Una sesión Programada no puede pausarse.",
        [(EstadoSesion.Programada, "Reanudar")] = "Una sesión Programada no puede reanudarse.",
        [(EstadoSesion.Programada, "Finalizar")] = "Una sesión Programada no puede finalizarse.",

        // EnPreparacion
        [(EstadoSesion.EnPreparacion, "Preparar")] = "La sesión ya se encuentra en preparación.",
        [(EstadoSesion.EnPreparacion, "Pausar")] =
            "Una sesión EnPreparacion no puede pausarse: debe iniciarse primero.",
        [(EstadoSesion.EnPreparacion, "Reanudar")] = "Una sesión EnPreparacion no puede reanudarse.",
        [(EstadoSesion.EnPreparacion, "Finalizar")] =
            "Una sesión EnPreparacion no puede finalizarse sin haberse iniciado.",

        // Activa
        [(EstadoSesion.Activa, "Preparar")] = "Una sesión Activa no puede volver a EnPreparacion.",
        [(EstadoSesion.Activa, "Iniciar")] = "La sesión ya se encuentra Activa.",
        [(EstadoSesion.Activa, "Reanudar")] = "La sesión ya se encuentra Activa.",

        // Pausada
        [(EstadoSesion.Pausada, "Preparar")] = "Una sesión Pausada no puede volver a EnPreparacion.",
        [(EstadoSesion.Pausada, "Iniciar")] = "Una sesión Pausada debe reanudarse, no iniciarse.",
        [(EstadoSesion.Pausada, "Pausar")] = "La sesión ya se encuentra Pausada.",
    };

    public static string ObtenerMensaje(EstadoSesion estado, string accion)
        => Mensajes.TryGetValue((estado, accion), out var mensaje)
            ? mensaje
            : ObtenerMensajeGenerico(estado);

    // Estados terminales y cualquier combinación no contemplada.
    private static string ObtenerMensajeGenerico(EstadoSesion estado) => estado switch
    {
        EstadoSesion.Finalizada => "Una sesión Finalizada no permite cambios de estado.",
        EstadoSesion.Cancelada => "Una sesión Cancelada no permite cambios de estado.",
        _ => "La transición solicitada no es válida para el estado actual de la sesión."
    };
}
