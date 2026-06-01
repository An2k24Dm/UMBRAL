namespace SesionesServicio.Dominio.Excepciones;

// HU34 — Se lanza cuando el usuario actual está autorizado por rol a
// usar el endpoint, pero no tiene permiso para ver la sesión solicitada
// según las reglas de visibilidad (por ejemplo, un Operador intentando
// ver una sesión creada por otro Operador). El middleware traduce esto
// a HTTP 403, no a 404.
public sealed class AccesoSesionNoPermitidoExcepcion : Exception
{
    public AccesoSesionNoPermitidoExcepcion(string mensaje) : base(mensaje) { }
}
