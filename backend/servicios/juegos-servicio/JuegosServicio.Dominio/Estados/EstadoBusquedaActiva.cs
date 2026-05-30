using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Estados;

// Patrón State — comportamiento de BusquedaTesoro cuando está Activa.
internal sealed class EstadoBusquedaActiva : IEstadoBusqueda
{
    public EstadoBusqueda Estado => EstadoBusqueda.Activa;

    public void Activar(Entidades.BusquedaTesoro busqueda) =>
        throw new ExcepcionDominio("La búsqueda del tesoro ya está activa.");

    public void Desactivar(Entidades.BusquedaTesoro busqueda)
    {
        busqueda.TransicionarEstado(EstadoBusqueda.Inactiva);
        busqueda.AgregarEventoInterno(new BusquedaArchivadaEvento(busqueda.Id));
    }

    public void ValidarEdicion(string accion) =>
        throw new ExcepcionDominio($"No se pueden {accion} a una búsqueda que está activa.");
}
