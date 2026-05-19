namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ErrorValidacion
{
    public string Campo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;

    public ErrorValidacion() { }

    public ErrorValidacion(string campo, string mensaje)
    {
        Campo = campo;
        Mensaje = mensaje;
    }
}
