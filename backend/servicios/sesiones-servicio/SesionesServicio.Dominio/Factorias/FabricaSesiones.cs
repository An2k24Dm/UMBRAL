using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Dominio.Factorias;

// Factory de Sesion. Devuelve la subclase concreta para que el
// manejador de aplicación no necesite hacer un switch por tipo. Si en
// el futuro aparece un nuevo tipo de sesión, se agrega un método aquí
// sin tocar el resto del flujo.
public static class FabricaSesiones
{
    public static SesionIndividual CrearIndividual(
        string nombre, string descripcion, DateTime fechaProgramada,
        string codigoAcceso, Guid operadorCreadorId, DateTime fechaCreacionUtc)
        => SesionIndividual.Crear(
            nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);

    public static SesionGrupal CrearGrupal(
        string nombre, string descripcion, DateTime fechaProgramada,
        string codigoAcceso, Guid operadorCreadorId, DateTime fechaCreacionUtc)
        => SesionGrupal.Crear(
            nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
}
