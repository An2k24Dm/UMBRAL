using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class MapeadorDetalleSesionIndividual : MapeadorDetalleSesionBase
{
    public override bool Soporta(Sesion sesion) => sesion is SesionIndividual;

    protected override void CompletarEspecifico(Sesion sesion, SesionDetalleDto dto)
    {
        var individual = (SesionIndividual)sesion;
        dto.MaximoParticipantes = individual.MaximoParticipantes;
        dto.ParticipantesIndividuales = individual.Participantes
            .OrderBy(p => p.FechaUnionSesion)
            .Select(p => new ParticipanteSesionDto
            {
                ParticipanteSesionId = p.Id,
                ParticipanteIdentidadId = p.ParticipanteIdentidadId,
                Puntaje = p.Puntaje.Valor,
                FechaUnion = p.FechaUnionSesion
            }).ToList();
    }
}
