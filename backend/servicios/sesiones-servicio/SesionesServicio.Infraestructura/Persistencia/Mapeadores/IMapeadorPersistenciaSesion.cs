using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

public interface IMapeadorPersistenciaSesion
{
    bool Soporta(string tipoSesion);

    void CompletarModelo(Sesion sesion, SesionModelo modelo);

    Sesion HaciaDominio(
        SesionModelo modelo,
        IReadOnlyList<SesionMision> misiones,
        IReadOnlyList<EjecucionActualSesion> secuenciaEtapas);
}
