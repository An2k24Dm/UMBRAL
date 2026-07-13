using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

// Entidad hija del agregado Ranking. Representa el puntaje de un equipo dentro
// del ranking de UNA sesión. Un equipo pertenece únicamente al Ranking de la
// sesión en la que fue creado (no existe reutilización global entre sesiones).
// No almacena el nombre del equipo (propiedad de sesiones-servicio). Su puntaje
// es un valor derivado que el agregado mantiene igual a la suma de los puntajes
// de sus participantes; por eso solo el agregado puede establecerlo.
public sealed class RankingEquipo
{
    public Guid Id { get; private set; }

    public Guid EquipoId { get; private set; }

    public Puntaje Puntaje { get; private set; } = Puntaje.Cero;

    private RankingEquipo() { }

    internal static RankingEquipo Crear(Guid equipoId)
    {
        if (equipoId == Guid.Empty)
            throw new RankingInvalidoExcepcion(
                "El identificador del equipo es obligatorio.");

        return new RankingEquipo
        {
            Id = Guid.NewGuid(),
            EquipoId = equipoId,
            Puntaje = Puntaje.Cero
        };
    }

    // El puntaje del equipo es siempre la suma de los puntajes de sus
    // participantes; el agregado Ranking lo recalcula y lo fija aquí.
    internal void EstablecerPuntaje(Puntaje puntaje) => Puntaje = puntaje;
}
