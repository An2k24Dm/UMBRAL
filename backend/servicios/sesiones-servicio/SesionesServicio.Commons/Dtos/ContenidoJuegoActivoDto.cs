namespace SesionesServicio.Commons.Dtos;

// Resumen del contenido (Trivia o Búsqueda del Tesoro) consultado a
// juegos-servicio. La capa de Aplicación lo usa para validar que el
// contenido existe y está Activo antes de permitir crear una sesión,
// y para guardar el snapshot del nombre dentro del agregado Sesion.
public sealed class ContenidoJuegoActivoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoJuego { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool EstaActivo { get; set; }
}
