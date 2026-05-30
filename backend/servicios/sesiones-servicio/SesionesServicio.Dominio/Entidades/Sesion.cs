using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Estados;

namespace SesionesServicio.Dominio.Entidades;

// HU33 — Agregado raíz Sesion.
//
// Una Sesion en vivo se crea a partir de contenido activo de juegos
// (Trivia o Búsqueda del Tesoro) y nace en estado Programada. La
// fábrica Crear se limita a construir la instancia con datos ya
// validados en la capa de Aplicación; los datos crudos (nombre vacío,
// enum inválido, fecha por defecto, etc.) no llegan hasta aquí.
//
// El cambio de estado se delega al patrón State (FabricaEstadoSesion),
// que centraliza las transiciones válidas. El setter de Estado se
// mantiene privado para que ninguna capa externa pueda saltarse las
// reglas.
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

    // Constructor privado para forzar el uso de la fábrica Crear.
    // Sin este constructor, EF Core no puede materializar la entidad.
    private Sesion() { }

    public static Sesion Crear(
        string nombre,
        TipoJuego tipoJuego,
        Guid contenidoJuegoId,
        ModoSesion modo,
        DateTime fechaProgramada,
        Guid creadaPorUsuarioId,
        DateTime fechaCreacionUtc)
        => new()
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

    // Rehidratación desde persistencia. La usa la Infraestructura para
    // reconstruir el agregado sin pasar por la fábrica (los datos ya
    // fueron validados al crear y persistir).
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
        => new()
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

    public void Preparar() => Estado = FabricaEstadoSesion.Obtener(Estado).Preparar();
    public void Iniciar() => Estado = FabricaEstadoSesion.Obtener(Estado).Iniciar();
    public void Pausar() => Estado = FabricaEstadoSesion.Obtener(Estado).Pausar();
    public void Reanudar() => Estado = FabricaEstadoSesion.Obtener(Estado).Reanudar();
    public void Finalizar() => Estado = FabricaEstadoSesion.Obtener(Estado).Finalizar();
    public void Cancelar() => Estado = FabricaEstadoSesion.Obtener(Estado).Cancelar();
}
