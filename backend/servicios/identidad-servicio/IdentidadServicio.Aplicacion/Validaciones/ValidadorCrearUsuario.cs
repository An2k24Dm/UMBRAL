using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Validaciones;

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

        dto.DatosContacto ??= new DatosContactoDto();
        dto.DatosContacto.Telefono = _reglas.NormalizarTelefono(dto.DatosContacto.Telefono);

        ValidarTipoUsuarioWeb(dto, resultado);

        _reglas.ValidarNombreUsuario(dto.NombreUsuario, resultado);
        _reglas.ValidarCorreo(dto.Correo, resultado);
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
