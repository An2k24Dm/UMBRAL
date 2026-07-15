namespace RankingServicio.Aplicacion.Puertos;

public interface IUsuarioActual
{
    bool EstaAutenticado();
    Guid? ObtenerId();
    IReadOnlyCollection<string> ObtenerRoles();
    bool TieneAlgunRol(params string[] roles);
}
