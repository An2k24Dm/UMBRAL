using PartidasServicio.Dominio.Estados;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Dominio.Entidades;

public sealed class Partida
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public IEstadoPartida Estado { get; private set; } = null!;
    public string NombreEstado => Estado.Nombre;
    public DateTime FechaCreacionUtc { get; private set; }
    public DateTime? FechaInicioUtc { get; private set; }
    public DateTime? FechaFinUtc { get; private set; }

    public bool EstaActiva => Estado is EstadoPartidaIniciada;

    private Partida() { }

    public static Partida Crear(Guid sesionId)
    {
        if (sesionId == Guid.Empty)
            throw new ExcepcionDominio("El identificador de la sesión es obligatorio.");

        return new Partida
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            Estado = new EstadoPartidaPendiente(),
            FechaCreacionUtc = DateTime.UtcNow
        };
    }

    public static Partida Reconstituir(
        Guid id, Guid sesionId, string estadoNombre,
        DateTime fechaCreacion, DateTime? fechaInicio, DateTime? fechaFin)
    {
        return new Partida
        {
            Id = id,
            SesionId = sesionId,
            Estado = ResolverEstado(estadoNombre),
            FechaCreacionUtc = fechaCreacion,
            FechaInicioUtc = fechaInicio,
            FechaFinUtc = fechaFin
        };
    }

    internal void CambiarEstado(IEstadoPartida nuevoEstado) => Estado = nuevoEstado;

    public void Iniciar(DateTime ahora)
    {
        Estado.Iniciar(this);
        FechaInicioUtc = ahora;
    }

    public void Pausar() => Estado.Pausar(this);

    public void Reanudar() => Estado.Reanudar(this);

    public void Finalizar(DateTime ahora)
    {
        Estado.Finalizar(this);
        FechaFinUtc = ahora;
    }

    public void Cancelar(DateTime ahora)
    {
        Estado.Cancelar(this);
        FechaFinUtc = ahora;
    }

    private static IEstadoPartida ResolverEstado(string nombre) => nombre switch
    {
        "Pendiente" => new EstadoPartidaPendiente(),
        "Iniciada" => new EstadoPartidaIniciada(),
        "Pausada" => new EstadoPartidaPausada(),
        "Finalizada" => new EstadoPartidaFinalizada(),
        "Cancelada" => new EstadoPartidaCancelada(),
        _ => throw new ExcepcionDominio($"Estado de partida desconocido: '{nombre}'.")
    };
}
