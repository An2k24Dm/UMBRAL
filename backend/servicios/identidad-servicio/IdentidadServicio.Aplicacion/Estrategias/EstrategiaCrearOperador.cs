using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class EstrategiaCrearOperador : IEstrategiaCreacionUsuario
{
    public bool PuedeCrear(TipoUsuario tipoUsuario) => tipoUsuario == TipoUsuario.Operador;

    public RolUsuario ObtenerRol() => RolUsuario.Operador;

    public Usuario CrearUsuarioDominio(CrearUsuarioDto dto, DateTime fechaRegistro)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoOperador))
            throw new DatosUsuarioInvalidosExcepcion("El código de operador es obligatorio.");

        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(dto);

        return Operador.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: dto.FechaNacimiento,
            codigoOperador: dto.CodigoOperador!,
            fechaRegistro: fechaRegistro);
    }

    public Task GuardarAsync(
        Usuario usuario, string idKeycloak,
        IRepositorioIdentidad repositorio, CancellationToken cancelacion)
    {
        return repositorio.GuardarOperadorAsync(
            (Operador)usuario, idKeycloak, cancelacion);
    }
}
