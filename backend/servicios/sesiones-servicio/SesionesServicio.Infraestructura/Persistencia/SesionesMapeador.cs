using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.Persistencia;

// Mapeo dominio ↔ persistencia. La columna `tipo_sesion` actúa como
// discriminador: el mapeador construye SesionIndividual o SesionGrupal
// según corresponda y agrupa correctamente los participantes (los del
// equipo se enrutan al Equipo, los individuales se quedan en la sesión).
public static class SesionesMapeador
{
    public static SesionModelo HaciaModelo(Sesion sesion)
    {
        var modelo = new SesionModelo
        {
            Id = sesion.Id,
            TipoSesion = sesion.TipoSesion,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Estado = sesion.Estado,
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso,
            OperadorCreadorId = sesion.OperadorCreadorId,
            FechaCreacion = sesion.FechaCreacion,
            FechaInicioUtc = sesion.FechaInicioUtc,
            FechaFinalizacionUtc = sesion.FechaFinalizacionUtc,
            Misiones = sesion.Misiones.Select(m => new SesionMisionModelo
            {
                Id = m.Id,
                SesionId = m.SesionId,
                MisionId = m.MisionId,
                Orden = m.Orden
            }).ToList()
        };

        switch (sesion)
        {
            case SesionIndividual individual:
                modelo.Participantes = individual.Participantes
                    .Select(MapearParticipante).ToList();
                break;
            case SesionGrupal grupal:
                modelo.Equipos = grupal.Equipos.Select(e => new EquipoModelo
                {
                    Id = e.Id,
                    SesionId = e.SesionId,
                    Nombre = e.Nombre,
                    LiderParticipanteId = e.LiderParticipanteId,
                    Puntaje = e.Puntaje,
                    FechaCreacion = e.FechaCreacion
                }).ToList();
                modelo.Participantes = grupal.Equipos
                    .SelectMany(e => e.Participantes)
                    .Select(MapearParticipante)
                    .ToList();
                break;
        }
        return modelo;
    }

    private static ParticipanteModelo MapearParticipante(Participante p) => new()
    {
        Id = p.Id,
        SesionId = p.SesionId,
        ParticipanteIdentidadId = p.ParticipanteIdentidadId,
        EquipoId = p.EquipoId,
        Puntaje = p.Puntaje,
        FechaUnionSesion = p.FechaUnionSesion,
        FechaUnionEquipo = p.FechaUnionEquipo
    };

    public static Sesion HaciaDominio(SesionModelo modelo)
    {
        var misiones = modelo.Misiones
            .Select(m => SesionMision.Rehidratar(m.Id, m.SesionId, m.MisionId, m.Orden))
            .ToList();

        if (string.Equals(modelo.TipoSesion, "Individual", StringComparison.OrdinalIgnoreCase))
        {
            var participantes = modelo.Participantes
                .Where(p => p.EquipoId is null)
                .Select(MapearAParticipante)
                .ToList();

            return SesionIndividual.Rehidratar(
                modelo.Id, modelo.Nombre, modelo.Descripcion, modelo.Estado,
                modelo.FechaProgramada, modelo.CodigoAcceso,
                modelo.OperadorCreadorId, modelo.FechaCreacion,
                modelo.FechaInicioUtc, modelo.FechaFinalizacionUtc,
                misiones, participantes);
        }

        // Modo grupal: los participantes se enrutan a su equipo.
        var integrantesPorEquipo = modelo.Participantes
            .Where(p => p.EquipoId is not null)
            .GroupBy(p => p.EquipoId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(MapearAParticipante).ToList());

        var equipos = modelo.Equipos.Select(em =>
        {
            integrantesPorEquipo.TryGetValue(em.Id, out var integrantes);
            return Equipo.Rehidratar(
                em.Id, em.SesionId, em.Nombre, em.LiderParticipanteId,
                em.Puntaje, em.FechaCreacion,
                integrantes ?? Enumerable.Empty<Participante>());
        }).ToList();

        return SesionGrupal.Rehidratar(
            modelo.Id, modelo.Nombre, modelo.Descripcion, modelo.Estado,
            modelo.FechaProgramada, modelo.CodigoAcceso,
            modelo.OperadorCreadorId, modelo.FechaCreacion,
            modelo.FechaInicioUtc, modelo.FechaFinalizacionUtc,
            misiones, equipos);
    }

    private static Participante MapearAParticipante(ParticipanteModelo p)
        => Participante.Rehidratar(
            p.Id, p.SesionId, p.ParticipanteIdentidadId,
            p.EquipoId, p.Puntaje, p.FechaUnionSesion, p.FechaUnionEquipo);
}
