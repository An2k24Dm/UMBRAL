namespace JuegosServicio.Commons.Dtos;

public sealed class ModificarMisionDto
{
    public string NuevoTitulo { get; set; } = default!;
    public string NuevaDescripcion { get; set; } = default!;
    public int NuevoTipo { get; set; }
    public string NuevaPistaClave { get; set; } = default!;
}
