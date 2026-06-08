namespace SesionesServicio.Dominio.Entidades;

// Relación intermedia Sesion ↔ Mision. La Mision vive en juegos-servicio,
// por eso aquí sólo guardamos el identificador y el orden con el que
// debe ejecutarse dentro de la sesión.
public sealed class SesionMision
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public Guid MisionId { get; private set; }
    public int Orden { get; private set; }

    private SesionMision() { }

    internal static SesionMision Crear(Guid sesionId, Guid misionId, int orden)
        => new()
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            MisionId = misionId,
            Orden = orden
        };

    public static SesionMision Rehidratar(Guid id, Guid sesionId, Guid misionId, int orden)
        => new() { Id = id, SesionId = sesionId, MisionId = misionId, Orden = orden };
}
