using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Fabricas;

public sealed class CreadorSesionIndividual : ICreadorSesion
{
    private static readonly string Modo = ModoSesion.Individual.ToString();

    public bool Soporta(string modo)
        => string.Equals(modo, Modo, StringComparison.OrdinalIgnoreCase);

    public Sesion Crear(
        string nombre,
        string descripcion,
        DateTime fechaProgramada,
        string codigoAcceso,
        Guid operadorCreadorId,
        DateTime fechaCreacionUtc)
        => SesionIndividual.Crear(
            nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
}
