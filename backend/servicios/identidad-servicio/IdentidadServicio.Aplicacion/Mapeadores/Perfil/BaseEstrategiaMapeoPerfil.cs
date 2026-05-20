using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Mapeadores.Perfil;

// Ayuda interna para no repetir el mapeo de los datos comunes en cada
// estrategia derivada. Mantiene el principio DRY sin filtrar conocimiento
// específico de los roles.
internal static class BaseEstrategiaMapeoPerfil
{
    public static void RellenarComunes(Usuario usuario, PerfilUsuarioDto dto)
    {
        dto.Id = usuario.Id;
        dto.NombreUsuario = usuario.NombreUsuario.Valor;
        dto.Correo = usuario.Correo.Valor;
        dto.Rol = usuario.Rol.ToString();
        dto.Estado = usuario.Estado.ToString();
        dto.Nombre = usuario.NombrePersona.Nombre;
        dto.Apellido = usuario.NombrePersona.Apellido;
        dto.DatosContacto = new DatosContactoDto
        {
            Direccion = usuario.DatosContacto.Direccion,
            Telefono = usuario.DatosContacto.Telefono
        };
        dto.Sexo = usuario.Sexo.ToString();
        dto.FechaNacimiento = usuario.FechaNacimiento;
        dto.FechaRegistro = usuario.FechaRegistro;
    }
}
