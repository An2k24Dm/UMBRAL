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
        if (busqueda.Mision is null)
            throw new ExcepcionDominio(
                "La búsqueda del tesoro debe tener una misión asignada para poder activarse.");

        busqueda.TransicionarEstado(EstadoBusqueda.Activa);
        busqueda.AgregarEventoInterno(
            new BusquedaActivadaEvento(busqueda.Id, busqueda.Nombre));
    }

    public void Desactivar(Entidades.BusquedaTesoro busqueda) =>
        throw new ExcepcionDominio("La búsqueda del tesoro ya está inactiva.");

    public void ValidarEdicion(string accion) { /* permitido */ }
}
