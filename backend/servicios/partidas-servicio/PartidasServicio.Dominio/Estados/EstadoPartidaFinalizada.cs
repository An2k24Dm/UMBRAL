using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Dominio.Estados;

public sealed class EstadoPartidaFinalizada : IEstadoPartida
{
    public string Nombre => "Finalizada";

    public void Iniciar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Iniciar));
    public void Pausar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Pausar));
    public void Reanudar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Reanudar));
    public void Finalizar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Finalizar));
    public void Cancelar(Partida partida) => throw new TransicionEstadoInvalidaExcepcion(Nombre, nameof(Cancelar));
}
