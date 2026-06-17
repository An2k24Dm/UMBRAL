using IdentidadServicio.Aplicacion.Validaciones;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IReglasValidacionUsuario
{
    void ValidarNombre(string? nombre, ResultadoValidacion resultado);
    void ValidarApellido(string? apellido, ResultadoValidacion resultado);
    void ValidarCorreo(string? correo, ResultadoValidacion resultado);
    void ValidarNombreUsuario(string? nombreUsuario, ResultadoValidacion resultado);
    void ValidarFechaNacimiento(DateTime? fechaNacimiento, ResultadoValidacion resultado);
    void ValidarTelefono(string? telefono, ResultadoValidacion resultado);
    void ValidarContrasena(string? contrasena, ResultadoValidacion resultado);
    void ValidarDireccion(string? direccion, ResultadoValidacion resultado);
    void ValidarSexo(string? sexo, ResultadoValidacion resultado);
    void ValidarAlias(string? alias, ResultadoValidacion resultado);
    string? NormalizarTelefono(string? telefono);
}
