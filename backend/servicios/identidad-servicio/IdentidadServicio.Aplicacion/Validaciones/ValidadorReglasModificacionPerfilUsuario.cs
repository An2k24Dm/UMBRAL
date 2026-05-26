using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

// Helper estático que aplica las reglas comunes de validación de edición
// parcial sobre cualquier DTO que herede de ModificarPerfilUsuarioDto.
//
// Lo usan tanto ValidadorModificarOperador (HU09) como
// ValidadorModificarParticipante (HU10). Centralizar aquí evita duplicar el
// patrón "si dto.X is not null → llamar regla X" y la verificación de
// coincidencia de contraseñas.
//
// Es un helper de Aplicación que delega en IReglasValidacionUsuario; NO
// accede a base de datos ni a Keycloak, ni mantiene estado.
internal static class ValidadorReglasModificacionPerfilUsuario
{
    public static void Validar(
        ModificarPerfilUsuarioDto dto,
        IReglasValidacionUsuario reglas,
        ResultadoValidacion resultado)
    {
        // Si llegó DatosContacto con teléfono, normalizamos a la forma canónica.
        if (dto.DatosContacto is not null)
            dto.DatosContacto.Telefono = reglas.NormalizarTelefono(dto.DatosContacto.Telefono);

        if (dto.NombreUsuario is not null)
            reglas.ValidarNombreUsuario(dto.NombreUsuario, resultado);

        if (dto.Correo is not null)
            reglas.ValidarCorreo(dto.Correo, resultado);

        if (dto.Nombre is not null)
            reglas.ValidarNombre(dto.Nombre, resultado);

        if (dto.Apellido is not null)
            reglas.ValidarApellido(dto.Apellido, resultado);

        if (dto.Sexo is not null)
            reglas.ValidarSexo(dto.Sexo, resultado);

        if (dto.FechaNacimiento is not null)
            reglas.ValidarFechaNacimiento(dto.FechaNacimiento, resultado);

        if (dto.DatosContacto?.Telefono is not null)
            reglas.ValidarTelefono(dto.DatosContacto.Telefono, resultado);

        if (dto.DatosContacto?.Direccion is not null)
            reglas.ValidarDireccion(dto.DatosContacto.Direccion, resultado);

        ValidarCambioContrasena(dto, reglas, resultado);
    }

    // Si ambos campos de contraseña vienen null, no se valida. Si llega
    // cualquiera con valor, se delega en ValidarContrasena (reglas comunes
    // con HU02/HU03) y se exige coincidencia entre NuevaContrasena y
    // ConfirmacionContrasena.
    private static void ValidarCambioContrasena(
        ModificarPerfilUsuarioDto dto,
        IReglasValidacionUsuario reglas,
        ResultadoValidacion resultado)
    {
        var solicita = dto.NuevaContrasena is not null || dto.ConfirmacionContrasena is not null;
        if (!solicita) return;

        reglas.ValidarContrasena(dto.NuevaContrasena, resultado);

        if (!string.Equals(dto.NuevaContrasena, dto.ConfirmacionContrasena, StringComparison.Ordinal))
        {
            resultado.Agregar(
                MensajesValidacionUsuario.CampoConfirmacionContrasena,
                MensajesValidacionUsuario.ContrasenasNoCoinciden);
        }
    }
}
