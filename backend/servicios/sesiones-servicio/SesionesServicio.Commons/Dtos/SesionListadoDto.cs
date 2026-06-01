namespace SesionesServicio.Commons.Dtos;

// HU34 — Fila del listado de sesiones.
//
// No incluye NombreContenido ni CreadaPorNombreUsuario ni CreadaPorRol:
// sesiones-servicio sólo guarda identificadores. El rol del creador,
// cuando hace falta para la regla de visibilidad, se resuelve en
// Aplicación consultando a identidad-servicio.
public sealed class SesionListadoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoJuego { get; set; } = string.Empty;
    public Guid ContenidoJuegoId { get; set; }
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public Guid CreadaPorUsuarioId { get; set; }

    // Grupos todavía no existen como agregado en sesiones-servicio.
    // HU34 lo pide en el listado, así que devolvemos 0 por compatibilidad
    // y dejamos preparada la propiedad para una historia posterior.
    public int NumeroGrupos { get; set; }
}
