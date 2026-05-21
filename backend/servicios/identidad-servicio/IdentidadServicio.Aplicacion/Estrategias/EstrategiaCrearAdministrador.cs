using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class EstrategiaCrearAdministrador : IEstrategiaCreacionUsuario
{
    private readonly IGeneradorCodigoUsuario _generador;

    public EstrategiaCrearAdministrador(IGeneradorCodigoUsuario generador)
    {
        _generador = generador;
    }

    public bool PuedeCrear(RolUsuario rol) => rol == RolUsuario.Administrador;

    public RolUsuario ObtenerRol() => RolUsuario.Administrador;

    public async Task<Usuario> CrearUsuarioDominioAsync(
        DatosCreacionUsuario datos, DateTime fechaRegistro, CancellationToken cancelacion)
    {
        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(datos);
        var codigoAdministrador = await _generador.GenerarCodigoAdministradorAsync(cancelacion);

        return Administrador.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: datos.FechaNacimiento,
            codigoAdministrador: codigoAdministrador,
            fechaRegistro: fechaRegistro);
    }

    public Task GuardarAsync(
        Usuario usuario, string idKeycloak,
        IRepositorioIdentidad repositorio, CancellationToken cancelacion)
    {
        return repositorio.GuardarAdministradorAsync(
            (Administrador)usuario, idKeycloak, cancelacion);
    }
}
