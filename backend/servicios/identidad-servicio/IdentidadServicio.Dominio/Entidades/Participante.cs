using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Dominio.Entidades;

public sealed class Participante : Usuario
{
    public string Alias { get; private set; } = string.Empty;

    private Participante() { }

    public Participante(
        Guid id,
        NombreUsuario nombreUsuario,
        Correo correo,
        EstadoUsuario estado,
        DateTime fechaRegistro,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string alias)
        : base(id, nombreUsuario, correo, RolUsuario.Participante, estado,
               fechaRegistro, nombrePersona, datosContacto, sexo, fechaNacimiento)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new DatosUsuarioInvalidosExcepcion("El alias del participante es obligatorio.");
        Alias = alias.Trim();
    }

    public static Participante Crear(
        NombreUsuario nombreUsuario,
        Correo correo,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string alias,
        DateTime fechaRegistro)
    {
        return new Participante(
            id: Guid.NewGuid(),
            nombreUsuario: nombreUsuario,
            correo: correo,
            estado: EstadoUsuario.Activo,
            fechaRegistro: fechaRegistro,
            nombrePersona: nombrePersona,
            datosContacto: datosContacto,
            sexo: sexo,
            fechaNacimiento: fechaNacimiento,
            alias: alias);
    }
}
