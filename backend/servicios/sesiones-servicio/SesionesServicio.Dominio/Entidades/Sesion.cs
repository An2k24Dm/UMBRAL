using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Estados;

namespace SesionesServicio.Dominio.Entidades;

public sealed class Sesion
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public TipoJuego TipoJuego { get; private set; }
    public Guid ContenidoJuegoId { get; private set; }
    public ModoSesion Modo { get; private set; }
    public EstadoSesion Estado { get; private set; }
    public DateTime FechaProgramada { get; private set; }
    public Guid CreadaPorUsuarioId { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    private IEstadoSesion _estadoActual = null!;

    private Sesion() { }

    public static Sesion Crear(
        string nombre,
        TipoJuego tipoJuego,
        Guid contenidoJuegoId,
        ModoSesion modo,
        DateTime fechaProgramada,
        Guid creadaPorUsuarioId,
        DateTime fechaCreacionUtc)
    {
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            TipoJuego = tipoJuego,
            ContenidoJuegoId = contenidoJuegoId,
            Modo = modo,
            Estado = EstadoSesion.Programada,
            FechaProgramada = fechaProgramada,
            CreadaPorUsuarioId = creadaPorUsuarioId,
            FechaCreacion = fechaCreacionUtc
        };
        sesion._estadoActual = FabricaEstadoSesion.Crear(EstadoSesion.Programada);
        return sesion;
    }

    public static Sesion Rehidratar(
        Guid id,
        string nombre,
        TipoJuego tipoJuego,
        Guid contenidoJuegoId,
        ModoSesion modo,
        EstadoSesion estado,
        DateTime fechaProgramada,
        Guid creadaPorUsuarioId,
        DateTime fechaCreacion)
    {
        var sesion = new Sesion
        {
            Id = id,
            Nombre = nombre,
            TipoJuego = tipoJuego,
            ContenidoJuegoId = contenidoJuegoId,
            Modo = modo,
            Estado = estado,
            FechaProgramada = fechaProgramada,
            CreadaPorUsuarioId = creadaPorUsuarioId,
            FechaCreacion = fechaCreacion
        };
        sesion._estadoActual = FabricaEstadoSesion.Crear(estado);
        return sesion;
    }

    internal void CambiarEstado(IEstadoSesion nuevoEstado)
    {
        _estadoActual = nuevoEstado;
        Estado = nuevoEstado.Estado;
    }

    public void Preparar() => _estadoActual.Preparar(this);
    public void Iniciar() => _estadoActual.Iniciar(this);
    public void Pausar() => _estadoActual.Pausar(this);
    public void Reanudar() => _estadoActual.Reanudar(this);
    public void Finalizar() => _estadoActual.Finalizar(this);
    public void Cancelar() => _estadoActual.Cancelar(this);
}
