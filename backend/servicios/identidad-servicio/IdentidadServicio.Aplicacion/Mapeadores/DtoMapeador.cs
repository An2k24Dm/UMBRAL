using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Mapeadores;

public static class DtoMapeador
{
    public static UsuarioAutenticadoDto AUsuarioAutenticado(Usuario usuario) => new()
    {
        Id = usuario.Id,
        NombreUsuario = usuario.NombreUsuario.Valor,
        Correo = usuario.Correo.Valor,
        Rol = usuario.Rol.ToString(),
        Estado = usuario.Estado.ToString()
    };

    public static SexoPersona ParsearSexo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return SexoPersona.Indefinido;
        return Enum.TryParse<SexoPersona>(valor, ignoreCase: true, out var sexo)
            ? sexo
            : SexoPersona.Indefinido;
    }

    public static string ResolverRutaPorRol(RolUsuario rol) => rol switch
    {
        RolUsuario.Administrador => "/administrador",
        RolUsuario.Operador      => "/operador/sesiones",
        RolUsuario.Participante  => "/participante/sesiones",
        _ => throw new RolNoValidoExcepcion()
    };
}
