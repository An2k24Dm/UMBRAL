using SesionesServicio.Dominio.Estrategias;

namespace SesionesServicio.Dominio.Abstract;

public interface IEstrategiaCalculoPuntajeTrivia
{
    int Calcular(ContextoCalculoPuntajeTrivia contexto);
}
