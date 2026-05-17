using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Aplicacion.Estrategias;

internal static class BaseEstrategia
{
    public static (NombreUsuario Nombre, Correo Correo, NombrePersona Persona,
                   DatosContacto Contacto, SexoPersona Sexo)
        ParsearDatosBasicos(CrearUsuarioDto dto)
    {
        return (
            NombreUsuario.Crear(dto.NombreUsuario),
            Correo.Crear(dto.Correo),
            NombrePersona.Crear(dto.Nombre, dto.Apellido),
            DatosContacto.Crear(dto.DatosContacto?.Direccion, dto.DatosContacto?.Telefono),
            DtoMapeador.ParsearSexo(dto.Sexo)
        );
    }
}
