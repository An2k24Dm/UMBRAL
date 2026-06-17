using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class MapeadorSesionDisponibleMovilGrupal : MapeadorSesionDisponibleMovilBase
{
    public override bool Soporta(Sesion sesion) => sesion is SesionGrupal;

    protected override void CompletarCapacidades(Sesion sesion, SesionDisponibleMovilDto dto)
    {
        var grupal = (SesionGrupal)sesion;
        dto.CantidadEquiposActuales = grupal.Equipos.Count;
        dto.CapacidadMaximaEquipos = grupal.MaximoEquipos;
    }
}
