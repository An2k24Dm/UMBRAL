using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

// Template Method: arma los campos comunes del SesionDetalleDto y delega en
// la subclase la parte específica del tipo de sesión (participantes
// individuales o equipos). Evita duplicar el mapeo común en cada estrategia.
public abstract class MapeadorDetalleSesionBase : IMapeadorDetalleSesion
{
    public abstract bool Soporta(Sesion sesion);

    public SesionDetalleDto Mapear(Sesion sesion)
    {
        var dto = new SesionDetalleDto
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
            FechaInicioUtc = sesion.FechaInicioUtc,
            FechaFinalizacionUtc = sesion.FechaFinalizacionUtc,
            DuracionMinutosLimite = sesion.DuracionMinutosLimite,
            Misiones = sesion.Misiones
                .OrderBy(m => m.Orden)
                .Select(m => new SesionMisionDto
                {
                    Id = m.Id,
                    MisionId = m.MisionId,
                    Orden = m.Orden
                }).ToList()
        };

        CompletarEspecifico(sesion, dto);
        return dto;
    }

    protected abstract void CompletarEspecifico(Sesion sesion, SesionDetalleDto dto);
}
