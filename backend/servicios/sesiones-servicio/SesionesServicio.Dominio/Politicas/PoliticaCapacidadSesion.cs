namespace SesionesServicio.Dominio.Politicas;

// Constantes de capacidad del ERS, centralizadas para que validador
// (Aplicación), invariantes (Dominio) y pruebas usen los mismos límites.
public static class PoliticaCapacidadSesion
{
    public const int MaximoMisionesPorSesion = 5;
    public const int MinimoMisionesPorSesion = 1;
    public const int MaximoParticipantesIndividual = 10;
    public const int MaximoEquiposPorSesion = 5;
    public const int MaximoParticipantesPorEquipo = 2;
}
