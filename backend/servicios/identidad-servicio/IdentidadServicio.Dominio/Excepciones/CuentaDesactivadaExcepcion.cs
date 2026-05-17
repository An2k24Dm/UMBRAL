namespace IdentidadServicio.Dominio.Excepciones;

public sealed class CuentaDesactivadaExcepcion : Exception
{
    public CuentaDesactivadaExcepcion()
        : base("La cuenta se encuentra desactivada.") { }
}
