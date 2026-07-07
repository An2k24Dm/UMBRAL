namespace PartidasServicio.Dominio.Estados;

public interface IEstadoPartida
{
    string Nombre { get; }
    void Iniciar(Entidades.Partida partida);
    void Pausar(Entidades.Partida partida);
    void Reanudar(Entidades.Partida partida);
    void Finalizar(Entidades.Partida partida);
    void Cancelar(Entidades.Partida partida);
}
