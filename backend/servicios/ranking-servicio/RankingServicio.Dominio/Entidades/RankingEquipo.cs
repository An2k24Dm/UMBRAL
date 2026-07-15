using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

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

    internal void EstablecerPuntaje(Puntaje puntaje) => Puntaje = puntaje;
}
