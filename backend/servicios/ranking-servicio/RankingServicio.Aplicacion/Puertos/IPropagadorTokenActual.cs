namespace RankingServicio.Aplicacion.Puertos;

// Expone el token Bearer del request actual para que los clientes HTTP hacia
// otros microservicios puedan reenviarlo al enriquecer las consultas.
public interface IPropagadorTokenActual
{
    string? ObtenerTokenActual();
}
