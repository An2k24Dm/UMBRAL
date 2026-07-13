namespace RankingServicio.Dominio.Abstract;

public interface IEstrategiaCalculoPuntaje<in TContexto>
{
    ObjetosValor.Puntaje Calcular(TContexto contexto);
}
