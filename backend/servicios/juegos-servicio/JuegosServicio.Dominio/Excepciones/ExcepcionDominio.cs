namespace JuegosServicio.Dominio.Excepciones;

// Base de las excepciones de regla de negocio del dominio. El
// middleware del API las traduce a HTTP 422 (REGLA_NEGOCIO). Las
// excepciones que necesitan datos extra (por ejemplo, identificadores
// para que el cliente sepa qué pasó) heredan de esta clase.
public class ExcepcionDominio : Exception
{
    public ExcepcionDominio(string mensaje) : base(mensaje) { }
}
