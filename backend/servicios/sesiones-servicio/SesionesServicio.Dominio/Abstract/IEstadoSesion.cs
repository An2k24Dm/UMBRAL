using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Abstract;

public interface IEstadoSesion
{
    EstadoSesion Estado { get; }

    void Preparar(Sesion sesion);
    void Iniciar(Sesion sesion);
    void Pausar(Sesion sesion);
    void Reanudar(Sesion sesion);
    void Finalizar(Sesion sesion);
    void Cancelar(Sesion sesion);
}
