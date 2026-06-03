namespace JuegosServicio.Commons.Dtos;

public sealed class CrearMisionDto
{
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    // 0=Baja, 1=Media, 2=Difícil — int para evitar dependencia en Commons→Dominio
    public int Dificultad { get; set; } = 1;
}
