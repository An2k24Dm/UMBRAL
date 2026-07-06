namespace PartidasServicio.Aplicacion.Validaciones;

public sealed class ResultadoValidacion
{
    private readonly List<ErrorValidacion> _errores = new();

    public IReadOnlyList<ErrorValidacion> Errores => _errores;
    public bool EsExitoso => _errores.Count == 0;

    public static ResultadoValidacion Exitoso() => new();

    public void Agregar(string campo, string mensaje)
        => _errores.Add(new ErrorValidacion { Campo = campo, Mensaje = mensaje });

    public void LanzarSiHayErrores()
    {
        if (!EsExitoso)
            throw new ExcepcionValidacion("Validación fallida.", _errores);
    }
}
