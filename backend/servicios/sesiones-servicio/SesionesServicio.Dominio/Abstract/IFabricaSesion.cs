using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Dominio.Abstract;

// Factory de sesiones. Selecciona el ICreadorSesion compatible con el modo
// recibido y delega la construcción. Los manejadores dependen de esta
// abstracción y nunca conocen las clases concretas de Sesion.
public interface IFabricaSesion
{
    Sesion Crear(
        string modo,
        string nombre,
        string descripcion,
        DateTime fechaProgramada,
        string codigoAcceso,
        Guid operadorCreadorId,
        DateTime fechaCreacionUtc);
}
