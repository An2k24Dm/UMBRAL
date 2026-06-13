using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class MapeadorListadoSesionIndividual : MapeadorListadoSesionBase
{
    public override bool Soporta(Sesion sesion) => sesion is SesionIndividual;

    protected override void CompletarConteos(Sesion sesion, SesionListadoDto dto)
    {
        var individual = (SesionIndividual)sesion;
        dto.CantidadParticipantes = individual.Participantes.Count;
        dto.CantidadEquipos = 0;
    }
}
