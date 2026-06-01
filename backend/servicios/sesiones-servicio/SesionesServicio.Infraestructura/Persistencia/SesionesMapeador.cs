using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.Persistencia;

// Mapeo dominio ↔ persistencia.
//
// Sesion expone setters privados (encapsulación del agregado), por eso
// la dirección modelo → dominio usa la fábrica explícita Rehidratar y
// no Mapster. La dirección dominio → modelo no necesita encapsulación;
// se hace con un proyector manual para mantener el código a la vista
// y evitar cualquier sorpresa por convenciones de mapeo automático.
//
// Rehidratar reconstruye además el ConcreteState del patrón State
// (via FabricaEstadoSesion). _estadoActual NO se persiste; sólo el
// enum Estado.
public static class SesionesMapeador
{
    public static SesionModelo HaciaModelo(Sesion sesion) => new()
    {
        Id = sesion.Id,
        Nombre = sesion.Nombre,
        TipoJuego = sesion.TipoJuego,
        ContenidoJuegoId = sesion.ContenidoJuegoId,
        Modo = sesion.Modo,
        Estado = sesion.Estado,
        FechaProgramada = sesion.FechaProgramada,
        CreadaPorUsuarioId = sesion.CreadaPorUsuarioId,
        FechaCreacion = sesion.FechaCreacion
    };

    public static Sesion HaciaDominio(SesionModelo modelo)
        => Sesion.Rehidratar(
            modelo.Id,
            modelo.Nombre,
            modelo.TipoJuego,
            modelo.ContenidoJuegoId,
            modelo.Modo,
            modelo.Estado,
            modelo.FechaProgramada,
            modelo.CreadaPorUsuarioId,
            modelo.FechaCreacion);
}
