namespace JuegosServicio.Dominio.Excepciones;

public sealed class MisionConSesionesVigentesExcepcion : ExcepcionDominio
{
    public MisionConSesionesVigentesExcepcion()
        : base("No se puede realizar esta operación porque la misión tiene sesiones activas.") { }
}
