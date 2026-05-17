using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Dominio.Entidades;

public sealed class Administrador : Usuario
{
    public string? CodigoAdministrador { get; private set; }

    private Administrador() { }

    public Administrador(
        Guid id,
        NombreUsuario nombreUsuario,
        Correo correo,
        EstadoUsuario estado,
        DateTime fechaRegistro,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string? codigoAdministrador)
        : base(id, nombreUsuario, correo, RolUsuario.Administrador, estado,
               fechaRegistro, nombrePersona, datosContacto, sexo, fechaNacimiento)
    {
        CodigoAdministrador = string.IsNullOrWhiteSpace(codigoAdministrador)
            ? null
            : codigoAdministrador.Trim();
    }

    public static Administrador Crear(
        NombreUsuario nombreUsuario,
        Correo correo,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string? codigoAdministrador,
        DateTime fechaRegistro)
    {
        return new Administrador(
            id: Guid.NewGuid(),
            nombreUsuario: nombreUsuario,
            correo: correo,
            estado: EstadoUsuario.Activo,
            fechaRegistro: fechaRegistro,
            nombrePersona: nombrePersona,
            datosContacto: datosContacto,
            sexo: sexo,
            fechaNacimiento: fechaNacimiento,
            codigoAdministrador: codigoAdministrador);
    }
}
