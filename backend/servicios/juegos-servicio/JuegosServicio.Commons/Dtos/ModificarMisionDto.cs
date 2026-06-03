namespace JuegosServicio.Commons.Dtos;

public sealed class ModificarMisionDto
{
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    // 0=Baja, 1=Media, 2=Difícil
    public int Dificultad { get; set; } = 1;
}
