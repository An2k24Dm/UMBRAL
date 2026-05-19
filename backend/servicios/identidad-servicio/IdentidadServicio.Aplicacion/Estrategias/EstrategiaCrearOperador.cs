using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
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

    public bool PuedeCrear(TipoUsuario tipoUsuario) => tipoUsuario == TipoUsuario.Operador;

    public RolUsuario ObtenerRol() => RolUsuario.Operador;

    public async Task<Usuario> CrearUsuarioDominioAsync(
        CrearUsuarioDto dto, DateTime fechaRegistro, CancellationToken cancelacion)
    {
        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(dto);
        var codigoOperador = await _generador.GenerarCodigoOperadorAsync(cancelacion);

        return Operador.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: dto.FechaNacimiento,
            codigoOperador: codigoOperador,
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
