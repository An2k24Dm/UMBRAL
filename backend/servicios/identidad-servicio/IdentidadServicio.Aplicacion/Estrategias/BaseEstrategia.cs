using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Aplicacion.Estrategias;

internal static class BaseEstrategia
{
    // Parsea el modelo interno de aplicación (DatosCreacionUsuario) en los
    // objetos de valor del dominio. Mantiene un único punto donde se aplica
    // la conversión, evitando que cada estrategia repita el código.
    public static (NombreUsuario Nombre, Correo Correo, NombrePersona Persona,
                   DatosContacto Contacto, SexoPersona Sexo)
        ParsearDatosBasicos(DatosCreacionUsuario datos)
    {
        return (
            NombreUsuario.Crear(datos.NombreUsuario),
            Correo.Crear(datos.Correo),
            NombrePersona.Crear(datos.Nombre, datos.Apellido),
            DatosContacto.Crear(
                datos.DatosContacto?.Direccion ?? string.Empty,
                datos.DatosContacto?.Telefono ?? string.Empty),
            DtoMapeador.ParsearSexo(datos.Sexo)
        );
    }
}
