using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Dominio.Entidades;

public sealed class Operador : Usuario
{
    public string CodigoOperador { get; private set; } = string.Empty;

    private Operador() { }

    public Operador(
        Guid id,
        NombreUsuario nombreUsuario,
        Correo correo,
        EstadoUsuario estado,
        DateTime fechaRegistro,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string codigoOperador)
        : base(id, nombreUsuario, correo, RolUsuario.Operador, estado,
               fechaRegistro, nombrePersona, datosContacto, sexo, fechaNacimiento)
    {
        if (string.IsNullOrWhiteSpace(codigoOperador))
            throw new DatosUsuarioInvalidosExcepcion("El código de operador es obligatorio.");
        CodigoOperador = codigoOperador.Trim();
    }

    public static Operador Crear(
        NombreUsuario nombreUsuario,
        Correo correo,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string codigoOperador,
        DateTime fechaRegistro)
    {
        return new Operador(
            id: Guid.NewGuid(),
            nombreUsuario: nombreUsuario,
            correo: correo,
            estado: EstadoUsuario.Activo,
            fechaRegistro: fechaRegistro,
            nombrePersona: nombrePersona,
            datosContacto: datosContacto,
            sexo: sexo,
            fechaNacimiento: fechaNacimiento,
            codigoOperador: codigoOperador);
    }
}
