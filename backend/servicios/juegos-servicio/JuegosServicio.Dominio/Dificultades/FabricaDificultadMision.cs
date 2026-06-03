using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Dificultades;

public static class FabricaDificultadMision
{
    public static IDificultadMision Obtener(NivelDificultad nivel) => nivel switch
    {
        NivelDificultad.Baja    => new DificultadBaja(),
        NivelDificultad.Media   => new DificultadMedia(),
        NivelDificultad.Dificil => new DificultadDificil(),
        _ => throw new ArgumentOutOfRangeException(nameof(nivel), $"Nivel de dificultad desconocido: {nivel}")
    };
}
