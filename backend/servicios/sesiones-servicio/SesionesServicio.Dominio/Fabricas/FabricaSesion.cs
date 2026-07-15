using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Fabricas;

public sealed class FabricaSesion : IFabricaSesion
{
    private readonly IEnumerable<ICreadorSesion> _creadores;

    public FabricaSesion(IEnumerable<ICreadorSesion> creadores)
    {
        _creadores = creadores;
    }

    public Sesion Crear(DatosCreacionSesion datos)
    {
        if (datos is null)
            throw new SesionInvalidaExcepcion("Los datos de creación son obligatorios.");
        if (string.IsNullOrWhiteSpace(datos.Modo))
            throw new SesionInvalidaExcepcion("El modo de la sesión es obligatorio.");

        var creador = _creadores.FirstOrDefault(c => c.Soporta(datos.Modo))
            ?? throw new SesionInvalidaExcepcion(
                $"No existe un creador de sesión para el modo '{datos.Modo}'.");

        return creador.Crear(datos);
    }

    public Sesion Reconstruir(DatosReconstruccionSesion datos)
    {
        if (datos is null)
            throw new SesionInvalidaExcepcion("Los datos de reconstrucción son obligatorios.");
        if (string.IsNullOrWhiteSpace(datos.Modo))
            throw new SesionInvalidaExcepcion("El modo de la sesión es obligatorio.");

        var creador = _creadores.FirstOrDefault(c => c.Soporta(datos.Modo))
            ?? throw new SesionInvalidaExcepcion(
                $"No existe un creador de sesión para el modo '{datos.Modo}'.");

        return creador.Reconstruir(datos);
    }
}
