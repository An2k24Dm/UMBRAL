using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class MapeadorSesionDisponibleMovilIndividual : MapeadorSesionDisponibleMovilBase
{
    public override bool Soporta(Sesion sesion) => sesion is SesionIndividual;

    protected override void CompletarCapacidades(Sesion sesion, SesionDisponibleMovilDto dto)
    {
        var individual = (SesionIndividual)sesion;
        dto.CantidadParticipantesActuales = individual.Participantes.Count;
        dto.CapacidadMaximaParticipantes = individual.MaximoParticipantes;
    }
}
