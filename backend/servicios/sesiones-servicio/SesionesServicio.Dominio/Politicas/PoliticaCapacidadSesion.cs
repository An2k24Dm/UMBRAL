using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Politicas;

public static class PoliticaCapacidadSesion
{
    public const int MaximoMisionesPorSesion = 5;
    public const int MinimoMisionesPorSesion = 1;
    public const int MinimoParticipantesIndividual = 1;
    public const int MinimoEquiposPorSesion = 1;
    public const int MinimoParticipantesPorEquipo = 2;

    // Valida la capacidad elegida para una sesión individual.
    public static void ValidarCapacidadIndividual(int maximoParticipantes)
    {
        if (maximoParticipantes < MinimoParticipantesIndividual)
            throw new SesionInvalidaExcepcion(
                $"El máximo de participantes debe ser al menos {MinimoParticipantesIndividual}.");
    }

    // Valida la capacidad elegida para una sesión grupal.
    public static void ValidarCapacidadGrupal(
        int maximoEquipos, int maximoParticipantesPorEquipo)
    {
        if (maximoEquipos < MinimoEquiposPorSesion)
            throw new SesionInvalidaExcepcion(
                $"El máximo de equipos debe ser al menos {MinimoEquiposPorSesion}.");
        if (maximoParticipantesPorEquipo < MinimoParticipantesPorEquipo)
            throw new SesionInvalidaExcepcion(
                $"El máximo de participantes por equipo debe ser al menos {MinimoParticipantesPorEquipo}.");
    }
}
