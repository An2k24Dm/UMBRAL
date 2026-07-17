using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

public sealed class RankingEquipo
{
    public Guid Id { get; private set; }
    public Guid EquipoId { get; private set; }
    public Puntaje Puntaje { get; private set; } = Puntaje.Cero;
    public int PuntosPenalizados { get; private set; }

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
            Puntaje = Puntaje.Cero,
            PuntosPenalizados = 0
        };
    }

    internal void EstablecerPuntaje(Puntaje puntaje) => Puntaje = puntaje;

    internal void AgregarPenalizacion(CantidadPenalizacion penalizacion)
        => PuntosPenalizados += penalizacion.Valor;
}
