using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

public sealed class MapeadorPersistenciaSesionGrupal : IMapeadorPersistenciaSesion
{
    private static readonly string Tipo = ModoSesion.Grupal.ToString();

    // Respaldo técnico (no regla de dominio) para filas anteriores a la
    // capacidad configurable, cuyas columnas pudieron quedar nulas. Las
    // sesiones nuevas siempre persisten su capacidad real.
    private const int CapacidadEquiposHistorica = 5;
    private const int CapacidadParticipantesPorEquipoHistorica = 2;

    public bool Soporta(string tipoSesion)
        => string.Equals(tipoSesion, Tipo, StringComparison.OrdinalIgnoreCase);

    public void CompletarModelo(Sesion sesion, SesionModelo modelo)
    {
        var grupal = (SesionGrupal)sesion;

        modelo.MaximoEquipos = grupal.MaximoEquipos;
        modelo.MaximoParticipantesPorEquipo = grupal.MaximoParticipantesPorEquipo;

        modelo.Equipos = grupal.Equipos.Select(e => new EquipoModelo
        {
            Id = e.Id,
            SesionId = e.SesionId,
            Nombre = e.Nombre,
            LiderParticipanteId = e.LiderParticipanteId,
            Puntaje = e.Puntaje,
            FechaCreacion = e.FechaCreacion
        }).ToList();

        // Los participantes de una sesión grupal viven dentro de sus equipos.
        modelo.Participantes = grupal.Equipos
            .SelectMany(e => e.Participantes)
            .Select(MapeoParticipantePersistencia.HaciaModelo)
            .ToList();
    }

    public Sesion HaciaDominio(SesionModelo modelo, IReadOnlyList<SesionMision> misiones)
    {
        // Los participantes se enrutan a su equipo por EquipoId.
        var integrantesPorEquipo = modelo.Participantes
            .Where(p => p.EquipoId is not null)
            .GroupBy(p => p.EquipoId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(MapeoParticipantePersistencia.HaciaDominio).ToList());

        var equipos = modelo.Equipos.Select(em =>
        {
            integrantesPorEquipo.TryGetValue(em.Id, out var integrantes);
            return Equipo.Rehidratar(
                em.Id, em.SesionId, em.Nombre, em.LiderParticipanteId,
                em.Puntaje, em.FechaCreacion,
                integrantes ?? Enumerable.Empty<Participante>());
        }).ToList();

        var maximoEquipos = modelo.MaximoEquipos
            ?? CapacidadEquiposHistorica;
        var maximoParticipantesPorEquipo = modelo.MaximoParticipantesPorEquipo
            ?? CapacidadParticipantesPorEquipoHistorica;

        return SesionGrupal.Rehidratar(
            modelo.Id, modelo.Nombre, modelo.Descripcion, modelo.Estado,
            modelo.FechaProgramada, modelo.CodigoAcceso,
            modelo.OperadorCreadorId, modelo.FechaCreacion,
            modelo.FechaInicioUtc, modelo.FechaFinalizacionUtc,
            maximoEquipos, maximoParticipantesPorEquipo,
            misiones, equipos);
    }
}
