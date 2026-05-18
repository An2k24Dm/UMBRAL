using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class EstrategiaCrearParticipante : IEstrategiaCreacionUsuario
{
    public bool PuedeCrear(TipoUsuario tipoUsuario) => tipoUsuario == TipoUsuario.Participante;

    public RolUsuario ObtenerRol() => RolUsuario.Participante;

    public Usuario CrearUsuarioDominio(CrearUsuarioDto dto, DateTime fechaRegistro)
    {
        if (string.IsNullOrWhiteSpace(dto.Alias))
            throw new DatosUsuarioInvalidosExcepcion("El alias del participante es obligatorio.");

        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(dto);

        return Participante.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: dto.FechaNacimiento,
            alias: dto.Alias!,
            fechaRegistro: fechaRegistro);
    }

    public Task GuardarAsync(
        Usuario usuario, string idKeycloak,
        IRepositorioIdentidad repositorio, CancellationToken cancelacion)
    {
        return repositorio.GuardarParticipanteAsync(
            (Participante)usuario, idKeycloak, cancelacion);
    }
}
