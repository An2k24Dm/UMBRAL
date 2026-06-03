using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Estados;

internal sealed class EstadoMisionActiva : IEstadoMision
{
    public EstadoMision Estado => EstadoMision.Activa;

    public void Activar(Entidades.Mision mision) =>
        throw new ExcepcionDominio("La misión ya está activa.");

    public void Desactivar(Entidades.Mision mision) =>
        mision.TransicionarEstado(EstadoMision.Inactiva);
}
