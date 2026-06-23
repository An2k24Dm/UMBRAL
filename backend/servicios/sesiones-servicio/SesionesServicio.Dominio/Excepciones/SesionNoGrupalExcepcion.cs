namespace SesionesServicio.Dominio.Excepciones;

// HU43 — Se lanza cuando se intenta consultar equipos de una sesión que no es
// grupal. El middleware lo traduce a HTTP 409.
public sealed class SesionNoGrupalExcepcion : Exception
{
    public SesionNoGrupalExcepcion(string mensaje) : base(mensaje) { }
}
