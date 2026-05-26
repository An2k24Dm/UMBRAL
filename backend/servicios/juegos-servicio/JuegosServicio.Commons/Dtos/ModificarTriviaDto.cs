namespace JuegosServicio.Commons.Dtos;

public sealed class ModificarTriviaDto
{
    public string NuevoNombre { get; init; } = default!;
    public string NuevaDescripcion { get; init; } = default!;
    public int NuevoTiempoLimitePorPregunta { get; init; }
}
