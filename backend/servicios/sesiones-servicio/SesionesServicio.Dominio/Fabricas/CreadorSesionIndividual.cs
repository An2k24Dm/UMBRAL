using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Fabricas;

public sealed class CreadorSesionIndividual : ICreadorSesion
{
    private static readonly string Modo = ModoSesion.Individual.ToString();

    public bool Soporta(string modo)
        => string.Equals(modo, Modo, StringComparison.OrdinalIgnoreCase);

    public Sesion Crear(DatosCreacionSesion datos)
    {
        var maximoParticipantes = datos.MaximoParticipantes
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de participantes para una sesión individual.");

        return SesionIndividual.Crear(
            datos.Nombre, datos.Descripcion, datos.FechaProgramada,
            datos.CodigoAcceso, datos.OperadorCreadorId, datos.FechaCreacionUtc,
            maximoParticipantes, datos.DuracionSegundosLimite);
    }

    public Sesion Reconstruir(DatosReconstruccionSesion datos)
    {
        var maximoParticipantes = datos.MaximoParticipantes
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de participantes para una sesión individual.");
        PoliticaCapacidadSesion.ValidarCapacidadIndividual(maximoParticipantes);

        // Se reconstruye preservando identidad. La sesión no tiene
        // participantes (lo garantiza el caso de uso antes de cambiar el modo).
        var sesion = SesionIndividual.Rehidratar(
            datos.Id, datos.Nombre, datos.Descripcion, datos.Estado,
            datos.FechaProgramada, datos.CodigoAcceso,
            datos.OperadorCreadorId, datos.FechaCreacionUtc,
            datos.FechaInicioUtc, datos.FechaFinalizacionUtc,
            maximoParticipantes,
            duracionSegundosLimite: datos.DuracionSegundosLimite,
            ejecucionActual: datos.EjecucionActual);
        sesion.AsignarMisiones(datos.MisionesIds);
        return sesion;
    }
}
