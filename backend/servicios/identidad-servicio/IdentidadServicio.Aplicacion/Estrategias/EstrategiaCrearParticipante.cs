using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class EstrategiaCrearParticipante : IEstrategiaCreacionUsuario
{
    public bool PuedeCrear(RolUsuario rol) => rol == RolUsuario.Participante;

    public RolUsuario ObtenerRol() => RolUsuario.Participante;

    public Task<Usuario> CrearUsuarioDominioAsync(
        CrearUsuarioDto dto, DateTime fechaRegistro, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(dto.Alias))
            throw new DatosUsuarioInvalidosExcepcion("El alias del participante es obligatorio.");

        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(dto);

        Usuario participante = Participante.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: dto.FechaNacimiento,
            alias: dto.Alias!,
            fechaRegistro: fechaRegistro);

        return Task.FromResult(participante);
    }

    public Task GuardarAsync(
        Usuario usuario, string idKeycloak,
        IRepositorioIdentidad repositorio, CancellationToken cancelacion)
    {
        return repositorio.GuardarParticipanteAsync(
            (Participante)usuario, idKeycloak, cancelacion);
    }
}
