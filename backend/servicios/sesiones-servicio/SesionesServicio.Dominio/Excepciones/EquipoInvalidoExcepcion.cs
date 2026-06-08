namespace SesionesServicio.Dominio.Excepciones;

public sealed class EquipoInvalidoExcepcion : Exception
{
    public EquipoInvalidoExcepcion(string mensaje) : base(mensaje) { }
}
