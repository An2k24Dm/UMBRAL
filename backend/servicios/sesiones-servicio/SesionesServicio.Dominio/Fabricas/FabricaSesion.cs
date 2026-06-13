using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Fabricas;

// Recibe todos los ICreadorSesion registrados y selecciona el que soporta
// el modo solicitado. No usa switch/if encadenados ni ternarios por tipo:
// la decisión la encapsula cada creador en su método Soporta.
public sealed class FabricaSesion : IFabricaSesion
{
    private readonly IEnumerable<ICreadorSesion> _creadores;

    public FabricaSesion(IEnumerable<ICreadorSesion> creadores)
    {
        _creadores = creadores;
    }

    public Sesion Crear(
        string modo,
        string nombre,
        string descripcion,
        DateTime fechaProgramada,
        string codigoAcceso,
        Guid operadorCreadorId,
        DateTime fechaCreacionUtc)
    {
        if (string.IsNullOrWhiteSpace(modo))
            throw new SesionInvalidaExcepcion("El modo de la sesión es obligatorio.");

        var creador = _creadores.FirstOrDefault(c => c.Soporta(modo))
            ?? throw new SesionInvalidaExcepcion(
                $"No existe un creador de sesión para el modo '{modo}'.");

        return creador.Crear(
            nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
    }
}
