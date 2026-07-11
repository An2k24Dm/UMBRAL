using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

public sealed class MapeadorPersistenciaSesionIndividual : IMapeadorPersistenciaSesion
{
    private static readonly string Tipo = ModoSesion.Individual.ToString();
    private const int CapacidadIndividualHistorica = 10;

    public bool Soporta(string tipoSesion)
        => string.Equals(tipoSesion, Tipo, StringComparison.OrdinalIgnoreCase);

    public void CompletarModelo(Sesion sesion, SesionModelo modelo)
    {
        var individual = (SesionIndividual)sesion;
        modelo.MaximoParticipantes = individual.MaximoParticipantes;
        modelo.Participantes = individual.Participantes
            .Select(MapeoParticipantePersistencia.HaciaModelo)
            .ToList();
    }

    public Sesion HaciaDominio(
        SesionModelo modelo,
        IReadOnlyList<SesionMision> misiones,
        IReadOnlyList<EjecucionActualSesion> secuenciaEtapas)
    {
        var participantes = modelo.Participantes
            .Where(p => p.EquipoId is null)
            .Select(MapeoParticipantePersistencia.HaciaDominio)
            .ToList();

        var maximoParticipantes = modelo.MaximoParticipantes
            ?? CapacidadIndividualHistorica;

        return SesionIndividual.Rehidratar(
            modelo.Id, modelo.Nombre, modelo.Descripcion, modelo.Estado,
            modelo.FechaProgramada, modelo.CodigoAcceso,
            modelo.OperadorCreadorId, modelo.FechaCreacion,
            modelo.FechaInicioUtc, modelo.FechaFinalizacionUtc,
            maximoParticipantes,
            misiones,
            participantes,
            modelo.DuracionSegundosLimite,
            MapeadorSesionesPersistencia.MapearEjecucionActual(modelo),
            secuenciaEtapas);
    }
}
