using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Dificultades;

public sealed class DificultadMedia : IDificultadMision
{
    public NivelDificultad Nivel => NivelDificultad.Media;
    public string Nombre => "Media";
    public string Descripcion => "Requiere conocimientos básicos del tema.";
}
