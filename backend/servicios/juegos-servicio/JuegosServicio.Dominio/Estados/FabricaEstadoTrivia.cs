using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Estados;

// Fábrica estática que devuelve la instancia de IEstadoTrivia
// correspondiente al valor del enum. Los estados son sin estado
// (stateless), por eso se cachean como singletons estáticos.
public static class FabricaEstadoTrivia
{
    private static readonly IEstadoTrivia Inactiva = new EstadoTriviaInactiva();
    private static readonly IEstadoTrivia Activa = new EstadoTriviaActiva();

    public static IEstadoTrivia Obtener(EstadoTrivia estado) => estado switch
    {
        EstadoTrivia.Inactiva => Inactiva,
        EstadoTrivia.Activa => Activa,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado,
            "Estado de trivia no soportado.")
    };
}
