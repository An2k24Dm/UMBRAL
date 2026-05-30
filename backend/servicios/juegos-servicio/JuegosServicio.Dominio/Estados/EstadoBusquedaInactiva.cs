using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Estados;

// Patrón State — comportamiento de BusquedaTesoro cuando está Inactiva.
internal sealed class EstadoBusquedaInactiva : IEstadoBusqueda
{
    public EstadoBusqueda Estado => EstadoBusqueda.Inactiva;

    public void Activar(Entidades.BusquedaTesoro busqueda)
    {
        if (busqueda.Etapas.Count == 0)
            throw new ExcepcionDominio(
                "La búsqueda del tesoro debe tener al menos una etapa para poder activarse.");

        var etapaSinMisiones = busqueda.Etapas.FirstOrDefault(e => e.Misiones.Count == 0);
        if (etapaSinMisiones is not null)
            throw new ExcepcionDominio(
                $"La etapa '{etapaSinMisiones.Titulo}' no tiene misiones. " +
                "Cada etapa debe tener al menos una misión.");

        busqueda.TransicionarEstado(EstadoBusqueda.Activa);
        busqueda.AgregarEventoInterno(
            new BusquedaActivadaEvento(busqueda.Id, busqueda.Nombre, busqueda.Etapas.Count));
    }

    public void Desactivar(Entidades.BusquedaTesoro busqueda) =>
        throw new ExcepcionDominio("La búsqueda del tesoro ya está inactiva.");

    public void ValidarEdicion(string accion) { /* permitido */ }
}
