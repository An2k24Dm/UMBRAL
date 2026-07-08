using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Validaciones.OperacionSesion;

/// <summary>
/// Regla autoritativa de aplicación para las acciones de juego (responder
/// trivia, enviar evidencia, completar etapa, sumar puntos, registrar avance).
/// El backend es la fuente de verdad: una acción de juego solo se permite
/// mientras la sesión está <see cref="EstadoSesion.Activa"/>. Si la sesión está
/// Pausada/Cancelada/Finalizada se rechaza con un mensaje claro.
///
/// Cuando se agreguen los endpoints/manejadores de juego (p. ej. responder
/// trivia o registrar puntaje), deben invocar <see cref="Validar"/> justo
/// antes de aplicar el avance, cargando la sesión desde el repositorio. Este
/// validador NO persiste auditoría ni crea tablas: solo protege la regla.
/// El frontend puede bloquear visualmente, pero la decisión vive aquí.
/// </summary>
public sealed class ValidadorAccionJuegoSesion
{
    public void Validar(Sesion sesion)
    {
        switch (sesion.Estado)
        {
            case EstadoSesion.Activa:
                return;

            case EstadoSesion.Pausada:
                throw new OperacionSesionInvalidaExcepcion(
                    "La sesión está pausada. No se pueden registrar acciones de juego " +
                    "hasta que sea reanudada.");

            case EstadoSesion.Cancelada:
                throw new OperacionSesionInvalidaExcepcion(
                    "La sesión fue cancelada. No se pueden registrar acciones de juego.");

            case EstadoSesion.Finalizada:
                throw new OperacionSesionInvalidaExcepcion(
                    "La sesión finalizó. No se pueden registrar acciones de juego.");

            default:
                // Programada / EnPreparacion: el juego todavía no comenzó.
                throw new OperacionSesionInvalidaExcepcion(
                    "La sesión aún no está activa. No se pueden registrar acciones de juego.");
        }
    }
}
