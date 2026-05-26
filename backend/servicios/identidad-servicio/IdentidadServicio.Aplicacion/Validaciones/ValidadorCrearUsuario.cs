using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Validaciones;

// HU02 — validador del registro administrativo de Operador / Administrador.
//
// Se concentra en las reglas particulares de este caso de uso:
//  * El TipoUsuario debe ser Administrador u Operador (Participante se
//    registra solo desde la app móvil mediante HU03).
//  * Contraseña obligatoria (la creación arma la cuenta en Keycloak).
// Todas las reglas comunes a un usuario del sistema (nombre, apellido, correo,
// nombre de usuario, fecha de nacimiento, teléfono, dirección, sexo) se
// delegan en IReglasValidacionUsuario para no duplicarlas con HU03.
//
// La comprobación de duplicados (correo / nombreUsuario / teléfono) vive en
// el manejador del caso de uso, porque requiere acceso al repositorio.
public sealed class ValidadorCrearUsuario : ValidadorBase<CrearUsuarioComando>
{
    private readonly IReglasValidacionUsuario _reglas;

    public ValidadorCrearUsuario(IReglasValidacionUsuario reglas)
    {
        _reglas = reglas;
    }

    protected override void ValidarSolicitud(
        CrearUsuarioComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos;

        // Normaliza el teléfono in-situ para que el manejador y la persistencia
        // reciban la forma canónica (solo dígitos, sin espacios ni guiones).
        dto.DatosContacto ??= new DatosContactoDto();
        dto.DatosContacto.Telefono = _reglas.NormalizarTelefono(dto.DatosContacto.Telefono);

        ValidarTipoUsuarioWeb(dto, resultado);

        _reglas.ValidarNombreUsuario(dto.NombreUsuario, resultado);
        _reglas.ValidarCorreo(dto.Correo, resultado);
        _reglas.ValidarContrasena(dto.Contrasena, resultado);
        _reglas.ValidarNombre(dto.Nombre, resultado);
        _reglas.ValidarApellido(dto.Apellido, resultado);
        _reglas.ValidarTelefono(dto.DatosContacto.Telefono, resultado);
        _reglas.ValidarDireccion(dto.DatosContacto.Direccion, resultado);
        _reglas.ValidarFechaNacimiento(dto.FechaNacimiento, resultado);
        _reglas.ValidarSexo(dto.Sexo, resultado);
    }

    private static void ValidarTipoUsuarioWeb(CrearUsuarioDto dto, ResultadoValidacion r)
    {
        if (dto.TipoUsuario != RolUsuario.Administrador && dto.TipoUsuario != RolUsuario.Operador)
            r.Agregar(MensajesValidacionUsuario.CampoTipoUsuario,
                MensajesValidacionUsuario.TipoUsuarioInvalidoWeb);
    }
}
