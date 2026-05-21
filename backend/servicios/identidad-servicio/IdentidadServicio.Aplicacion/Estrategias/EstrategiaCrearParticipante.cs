using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class EstrategiaCrearParticipante : IEstrategiaCreacionUsuario
{
    public bool PuedeCrear(RolUsuario rol) => rol == RolUsuario.Participante;

    public RolUsuario ObtenerRol() => RolUsuario.Participante;

    public Task<Usuario> CrearUsuarioDominioAsync(
        DatosCreacionUsuario datos, DateTime fechaRegistro, CancellationToken cancelacion)
    {
        // Alias vive en DatosCreacionUsuario y solo aplica para Participante.
        // El validador de HU03 ya garantiza no nulo/no vacío; la verificación
        // aquí actúa como red de seguridad si alguien construye DatosCreacionUsuario
        // por otra vía sin pasar por el validador.
        if (string.IsNullOrWhiteSpace(datos.Alias))
            throw new DatosUsuarioInvalidosExcepcion("El alias del participante es obligatorio.");

        var (nombre, correo, persona, contacto, sexo) = BaseEstrategia.ParsearDatosBasicos(datos);

        Usuario participante = Participante.Crear(
            nombreUsuario: nombre,
            correo: correo,
            nombrePersona: persona,
            datosContacto: contacto,
            sexo: sexo,
            fechaNacimiento: datos.FechaNacimiento,
            alias: datos.Alias!,
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
