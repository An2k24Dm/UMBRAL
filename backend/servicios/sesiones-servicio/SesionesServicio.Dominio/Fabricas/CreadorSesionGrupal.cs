using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Fabricas;

public sealed class CreadorSesionGrupal : ICreadorSesion
{
    private static readonly string Modo = ModoSesion.Grupal.ToString();

    public bool Soporta(string modo)
        => string.Equals(modo, Modo, StringComparison.OrdinalIgnoreCase);

    public Sesion Crear(DatosCreacionSesion datos)
    {
        var maximoEquipos = datos.MaximoEquipos
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de equipos para una sesión grupal.");
        var maximoParticipantesPorEquipo = datos.MaximoParticipantesPorEquipo
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de participantes por equipo para una sesión grupal.");

        return SesionGrupal.Crear(
            datos.Nombre, datos.Descripcion, datos.FechaProgramada,
            datos.CodigoAcceso, datos.OperadorCreadorId, datos.FechaCreacionUtc,
            maximoEquipos, maximoParticipantesPorEquipo, datos.DuracionMinutosLimite);
    }

    public Sesion Reconstruir(DatosReconstruccionSesion datos)
    {
        var maximoEquipos = datos.MaximoEquipos
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de equipos para una sesión grupal.");
        var maximoParticipantesPorEquipo = datos.MaximoParticipantesPorEquipo
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de participantes por equipo para una sesión grupal.");
        PoliticaCapacidadSesion.ValidarCapacidadGrupal(maximoEquipos, maximoParticipantesPorEquipo);

        // Se reconstruye preservando identidad. La sesión no tiene equipos
        // (lo garantiza el caso de uso antes de cambiar el modo).
        var sesion = SesionGrupal.Rehidratar(
            datos.Id, datos.Nombre, datos.Descripcion, datos.Estado,
            datos.FechaProgramada, datos.CodigoAcceso,
            datos.OperadorCreadorId, datos.FechaCreacionUtc,
            datos.FechaInicioUtc, datos.FechaFinalizacionUtc,
            maximoEquipos, maximoParticipantesPorEquipo,
            duracionMinutosLimite: datos.DuracionMinutosLimite);
        sesion.AsignarMisiones(datos.MisionesIds);
        return sesion;
    }
}
