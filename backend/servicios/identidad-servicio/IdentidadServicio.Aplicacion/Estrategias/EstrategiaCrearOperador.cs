using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class EstrategiaCrearOperador : IEstrategiaCreacionUsuario
{
    private readonly IGeneradorCodigoUsuario _generador;

    public EstrategiaCrearOperador(IGeneradorCodigoUsuario generador)
    {
        _generador = generador;
    }

    public bool PuedeCrear(RolUsuario rol) => rol == RolUsuario.Operador;

    public RolUsuario ObtenerRol() => RolUsuario.Operador;

    public async Task<Usuario> CrearUsuarioDominioAsync(
        DatosCreacionUsuario datos, DateTime fechaRegistro, CancellationToken cancelacion)
    {
        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(datos);
        var codigoOperador = await _generador.GenerarCodigoOperadorAsync(cancelacion);

        return Operador.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: datos.FechaNacimiento,
            codigoOperador: codigoOperador,
            fechaRegistro: fechaRegistro);
    }

}
