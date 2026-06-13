using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

// Template Method para SesionDisponibleMovilDto: campos comunes + capacidades
// específicas del tipo (solo se rellenan las que aplican al modo).
public abstract class MapeadorSesionDisponibleMovilBase : IMapeadorSesionDisponibleMovil
{
    public abstract bool Soporta(Sesion sesion);

    public SesionDisponibleMovilDto Mapear(Sesion sesion)
    {
        var dto = new SesionDisponibleMovilDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Modo = sesion.TipoSesion,
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CantidadMisiones = sesion.Misiones.Count
        };

        CompletarCapacidades(sesion, dto);
        return dto;
    }

    protected abstract void CompletarCapacidades(Sesion sesion, SesionDisponibleMovilDto dto);
}
