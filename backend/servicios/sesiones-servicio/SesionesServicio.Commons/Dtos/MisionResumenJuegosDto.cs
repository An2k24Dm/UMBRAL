namespace SesionesServicio.Commons.Dtos;

// Espejo liviano del MisionResumenDto que expone juegos-servicio. Sólo
// los campos que sesiones-servicio necesita: identificador, estado,
// cantidad de etapas (para impedir misiones vacías) y datos de
// presentación que aparecen en el detalle de la sesión.
public sealed class MisionResumenJuegosDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Dificultad { get; set; } = string.Empty;
    public int TotalEtapas { get; set; }
    public int TiempoTotalSegundos { get; set; }
}
