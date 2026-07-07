using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Dominio.Estados;

public sealed class EstadoPartidaIniciada : IEstadoPartida
{
    public string Nombre => "Iniciada";

    public void Iniciar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Iniciar));
    public void Pausar(Partida partida) => partida.CambiarEstado(new EstadoPartidaPausada());
    public void Reanudar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Reanudar));
    public void Finalizar(Partida partida) => partida.CambiarEstado(new EstadoPartidaFinalizada());
    public void Cancelar(Partida partida) => partida.CambiarEstado(new EstadoPartidaCancelada());
}
