using RankingServicio.Aplicacion.Puertos;

using RankingServicio.Commons.Dtos.ServiciosExternos;

namespace RankingServicio.Aplicacion.Consultas;

// Resuelve el alias de presentación a partir de los datos enriquecidos desde
// identidad-servicio. Si no hay alias, usa "Nombre Apellido"; como último
// recurso el identificador, para no romper la presentación cuando el
// enriquecimiento no está disponible.
internal static class ResolucionAlias
{
    public static string Resolver(
        Guid participanteIdentidadId,
        IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto> datos)
    {
        if (datos.TryGetValue(participanteIdentidadId, out var d))
        {
            if (!string.IsNullOrWhiteSpace(d.Alias))
                return d.Alias;

            var nombre = $"{d.Nombre} {d.Apellido}".Trim();
            if (!string.IsNullOrWhiteSpace(nombre))
                return nombre;
        }

        return participanteIdentidadId.ToString();
    }
}
