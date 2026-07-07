using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Dominio.Estados;

public sealed class EstadoPartidaPausada : IEstadoPartida
{
    public string Nombre => "Pausada";

    public void Iniciar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Iniciar));
    public void Pausar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Pausar));
    public void Reanudar(Partida partida) => partida.CambiarEstado(new EstadoPartidaIniciada());
    public void Finalizar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Finalizar));
    public void Cancelar(Partida partida) => partida.CambiarEstado(new EstadoPartidaCancelada());
}
