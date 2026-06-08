using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Politicas;

public static class PoliticaProgramacionSesion
{
    public static void ValidarFechaProgramada(
        DateTime fechaProgramada,
        DateTime fechaActualUtc)
    {
        if (fechaProgramada <= fechaActualUtc)
        {
            throw new SesionInvalidaExcepcion(
                "La sesión no puede programarse para una fecha y hora que ya pasó.");
        }
    }
}
