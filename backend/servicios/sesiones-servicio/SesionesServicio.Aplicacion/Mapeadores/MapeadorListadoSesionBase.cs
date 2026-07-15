using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

// Template Method para SesionListadoDto: mapea los campos comunes y deja a
// la subclase rellenar los conteos propios de su tipo (participantes/equipos).
public abstract class MapeadorListadoSesionBase : IMapeadorListadoSesion
{
    public abstract bool Soporta(Sesion sesion);

    public SesionListadoDto Mapear(Sesion sesion)
    {
        var dto = new SesionListadoDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Modo = sesion.TipoSesion,
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso,
            OperadorCreadorId = sesion.OperadorCreadorId,
            FechaCreacion = sesion.FechaCreacion,
            CantidadMisiones = sesion.Misiones.Count
        };

        CompletarConteos(sesion, dto);
        return dto;
    }

    protected abstract void CompletarConteos(Sesion sesion, SesionListadoDto dto);
}
