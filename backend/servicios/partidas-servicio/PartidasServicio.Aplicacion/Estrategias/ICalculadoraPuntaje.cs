namespace PartidasServicio.Aplicacion.Estrategias;

public interface ICalculadoraPuntaje
{
    int Calcular(int puntajeBase, long tiempoTardadoMs, long tiempoLimiteMs);
}
