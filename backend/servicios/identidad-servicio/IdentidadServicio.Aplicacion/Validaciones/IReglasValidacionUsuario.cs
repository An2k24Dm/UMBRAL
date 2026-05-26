namespace IdentidadServicio.Aplicacion.Validaciones;

// Conjunto de reglas comunes de validación de usuario reutilizables por los
// validadores específicos (HU02 / HU03 / HU09).
//
// Cada método recibe el valor a validar y el ResultadoValidacion sobre el que
// agregar los posibles errores. Esto permite componer varias reglas sobre el
// mismo resultado sin que los validadores tengan que duplicar mensajes ni
// expresiones regulares.
//
// Los nombres de campo y los mensajes asociados viven en
// MensajesValidacionUsuario para mantener centralizada la copy de los
// mensajes mostrados al usuario.
public interface IReglasValidacionUsuario
{
    // -------- Reglas listadas explícitamente en la HU de refactor --------
    void ValidarNombre(string? nombre, ResultadoValidacion resultado);
    void ValidarApellido(string? apellido, ResultadoValidacion resultado);
    void ValidarCorreo(string? correo, ResultadoValidacion resultado);
    void ValidarNombreUsuario(string? nombreUsuario, ResultadoValidacion resultado);
    void ValidarFechaNacimiento(DateTime? fechaNacimiento, ResultadoValidacion resultado);
    void ValidarTelefono(string? telefono, ResultadoValidacion resultado);

    // -------- Reglas comunes adicionales reutilizadas por HU02/HU03 -------
    // Se centralizan aquí para no duplicar texto/regex entre validadores.
    void ValidarContrasena(string? contrasena, ResultadoValidacion resultado);
    void ValidarDireccion(string? direccion, ResultadoValidacion resultado);
    void ValidarSexo(string? sexo, ResultadoValidacion resultado);

    // Utilitario expuesto para que los validadores normalicen el teléfono
    // antes de validarlo (y que el handler reciba el valor canónico).
    string? NormalizarTelefono(string? telefono);
}
