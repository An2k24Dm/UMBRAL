namespace IdentidadServicio.Aplicacion.Validaciones;

// Resultado de la regla "un usuario autenticado solo puede usar el sistema si
// su cuenta está Activa". No conoce HTTP: la Presentación traduce este
// resultado a la respuesta correspondiente.
public sealed record ResultadoAccesoUsuarioActivo(
    bool PuedeAcceder,
    string? Codigo = null,
    string? Mensaje = null)
{
    public static ResultadoAccesoUsuarioActivo Permitido()
        => new(true);

    public static ResultadoAccesoUsuarioActivo Bloqueado(
        string codigo,
        string mensaje)
        => new(false, codigo, mensaje);
}
