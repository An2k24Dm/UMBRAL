using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

public sealed class MapeadorPersistenciaSesionGrupal : IMapeadorPersistenciaSesion
{
    private static readonly string Tipo = ModoSesion.Grupal.ToString();

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
            Nombre = e.Nombre.Valor,
            LiderParticipanteId = e.LiderParticipanteId,
            Puntaje = e.Puntaje.Valor,
            SnapshotRankingUtc = e.SnapshotRankingUtc,
            Tipo = e.Tipo,
            ContrasenaHash = e.ContrasenaHash?.Valor,
            CapacidadMaxima = e.CapacidadMaxima,
            FechaCreacion = e.FechaCreacion
        }).ToList();

        modelo.Participantes = grupal.Equipos
            .SelectMany(e => e.Participantes)
            .Select(MapeoParticipantePersistencia.HaciaModelo)
            .ToList();
    }

    public Sesion HaciaDominio(
        SesionModelo modelo,
        IReadOnlyList<SesionMision> misiones,
        IReadOnlyList<EjecucionActualSesion> secuenciaEtapas)
    {
        var integrantesPorEquipo = modelo.Participantes
            .Where(p => p.EquipoId is not null)
            .GroupBy(p => p.EquipoId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(MapeoParticipantePersistencia.HaciaDominio).ToList());

        var equipos = modelo.Equipos.Select(em =>
        {
            integrantesPorEquipo.TryGetValue(em.Id, out var integrantes);
            var capacidad = em.CapacidadMaxima > 0
                ? em.CapacidadMaxima
                : CapacidadParticipantesPorEquipoHistorica;
            return Equipo.Rehidratar(
                em.Id, em.SesionId, em.Nombre, em.LiderParticipanteId,
                em.Puntaje, em.Tipo, em.ContrasenaHash, capacidad, em.FechaCreacion,
                integrantes ?? Enumerable.Empty<Participante>(),
                em.SnapshotRankingUtc);
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
            misiones,
            equipos,
            modelo.DuracionSegundosLimite,
            MapeadorSesionesPersistencia.MapearEjecucionActual(modelo),
            secuenciaEtapas);
    }
}
