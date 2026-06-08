namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ResultadoValidacion
{
    public bool EsValido => Errores.Count == 0;
    public List<ErrorValidacion> Errores { get; } = new();

    public static ResultadoValidacion Exitoso() => new();

    public void Agregar(string campo, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(campo) || string.IsNullOrWhiteSpace(mensaje))
            return;

        if (Errores.Any(e => e.Campo == campo && e.Mensaje == mensaje))
            return;

        Errores.Add(new ErrorValidacion(campo, mensaje));
    }

    public void LanzarSiHayErrores(string mensajeGeneral = "Existen errores de validación.")
    {
        if (!EsValido)
            throw new ExcepcionValidacion(mensajeGeneral, Errores);
    }
}
