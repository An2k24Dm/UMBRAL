using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Dificultades;

public sealed class DificultadDificil : IDificultadMision
{
    public NivelDificultad Nivel => NivelDificultad.Dificil;
    public string Nombre => "Difícil";
    public string Descripcion => "Diseñada para participantes con experiencia avanzada.";
}
