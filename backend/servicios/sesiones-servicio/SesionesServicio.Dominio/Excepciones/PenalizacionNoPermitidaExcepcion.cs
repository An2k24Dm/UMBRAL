namespace SesionesServicio.Dominio.Excepciones;

// HU52 — La penalización no está permitida por el estado de la sesión (solo
// Activa o Pausada). Conflicto de negocio: se mapea a 409 Conflict.
public sealed class PenalizacionNoPermitidaExcepcion : Exception
{
    public PenalizacionNoPermitidaExcepcion(string mensaje) : base(mensaje) { }
}
