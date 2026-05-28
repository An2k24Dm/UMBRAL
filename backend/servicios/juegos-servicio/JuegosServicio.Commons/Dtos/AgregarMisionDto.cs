namespace JuegosServicio.Commons.Dtos;

public sealed class AgregarMisionDto
{
    public string Titulo { get; init; } = default!;
    public string Descripcion { get; init; } = default!;
    public int Tipo { get; init; }
    public string PistaClave { get; init; } = default!;
}
