using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

internal static class ValidadorReglasModificacionPerfilUsuario
{
    public static void Validar(
        ModificarPerfilUsuarioDto dto,
        IReglasValidacionUsuario reglas,
        ResultadoValidacion resultado)
    {
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
    }
}
