using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class MapeadorListadoSesionGrupal : MapeadorListadoSesionBase
{
    public override bool Soporta(Sesion sesion) => sesion is SesionGrupal;

    protected override void CompletarConteos(Sesion sesion, SesionListadoDto dto)
    {
        var grupal = (SesionGrupal)sesion;
        dto.CantidadEquipos = grupal.Equipos.Count;
        dto.CantidadParticipantes = 0;
    }
}
