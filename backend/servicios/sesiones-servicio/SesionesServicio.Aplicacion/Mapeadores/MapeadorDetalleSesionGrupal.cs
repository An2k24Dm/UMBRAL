using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class MapeadorDetalleSesionGrupal : MapeadorDetalleSesionBase
{
    public override bool Soporta(Sesion sesion) => sesion is SesionGrupal;

    protected override void CompletarEspecifico(Sesion sesion, SesionDetalleDto dto)
    {
        var grupal = (SesionGrupal)sesion;
        dto.MaximoEquipos = grupal.MaximoEquipos;
        dto.MaximoParticipantesPorEquipo = grupal.MaximoParticipantesPorEquipo;
        dto.Equipos = grupal.Equipos
            .Select(e => new EquipoSesionDto
            {
                Id = e.Id,
                Nombre = e.Nombre.Valor,
                Tipo = e.Tipo.ToString(),
                PuntajeActual = e.Puntaje,
                CapacidadMaxima = e.CapacidadMaxima,
                FechaCreacion = e.FechaCreacion,
                LiderParticipanteId = e.LiderParticipanteId,
                Participantes = e.Participantes
                    .Select(p => new ParticipanteEquipoDto
                    {
                        Id = p.Id,
                        ParticipanteId = p.ParticipanteIdentidadId,
                        FechaUnion = p.FechaUnionEquipo ?? p.FechaUnionSesion
                    }).ToList()
            }).ToList();
    }
}
