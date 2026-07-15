namespace SesionesServicio.Dominio.Excepciones;

// HU43 — Se lanza cuando el equipo solicitado no existe en la sesión indicada.
// El middleware lo traduce a HTTP 404.
public sealed class EquipoNoEncontradoExcepcion : Exception
{
    public EquipoNoEncontradoExcepcion(string mensaje) : base(mensaje) { }
}
