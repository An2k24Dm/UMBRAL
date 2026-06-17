using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.Dominio.Abstract;

// Factory de sesiones. Selecciona el ICreadorSesion compatible con el modo
// recibido y delega la construcción. Los manejadores dependen de esta
// abstracción y nunca conocen las clases concretas de Sesion.
public interface IFabricaSesion
{
    Sesion Crear(DatosCreacionSesion datos);

    // Reconstruye una sesión como el modo indicado en los datos, conservando
    // su identidad. Selecciona el creador compatible sin que el llamador
    // conozca las clases concretas de Sesion.
    Sesion Reconstruir(DatosReconstruccionSesion datos);
}
