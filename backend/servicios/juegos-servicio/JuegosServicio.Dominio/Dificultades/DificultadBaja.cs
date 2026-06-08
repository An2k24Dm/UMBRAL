using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Dificultades;

public sealed class DificultadBaja : IDificultadMision
{
    public NivelDificultad Nivel => NivelDificultad.Baja;
    public string Nombre => "Baja";
    public string Descripcion => "Apta para participantes sin experiencia previa.";
}
