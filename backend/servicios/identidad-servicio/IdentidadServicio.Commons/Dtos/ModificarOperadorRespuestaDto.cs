namespace IdentidadServicio.Commons.Dtos;

// HU09 — respuesta de la edición parcial del Operador. Devuelve el perfil
// completo actualizado (para que el frontend pueda refrescar el detalle) junto
// con la lista de campos que efectivamente cambiaron y un indicador de si
// hubo o no cambios reales.
public sealed class ModificarOperadorRespuestaDto
{
    public bool HuboCambios { get; set; }
    public IReadOnlyList<string> CamposActualizados { get; set; } = Array.Empty<string>();
    public string Mensaje { get; set; } = string.Empty;
    public PerfilOperadorDto Operador { get; set; } = new();
}
