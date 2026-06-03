using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Estados;

internal sealed class EstadoMisionInactiva : IEstadoMision
{
    public EstadoMision Estado => EstadoMision.Inactiva;

    public void Activar(Entidades.Mision mision)
    {
        if (!mision.Etapas.Any())
            throw new ExcepcionDominio(
                "La misión debe tener al menos una etapa para poder activarse.");

        mision.TransicionarEstado(EstadoMision.Activa);
    }

    public void Desactivar(Entidades.Mision mision) =>
        throw new ExcepcionDominio("La misión ya está inactiva.");
}
