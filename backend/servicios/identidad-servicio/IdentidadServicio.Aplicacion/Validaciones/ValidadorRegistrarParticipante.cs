using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorRegistrarParticipante
    : ValidadorBase<RegistrarParticipanteComando>
{
    private readonly IReglasValidacionUsuario _reglas;

    public ValidadorRegistrarParticipante(IReglasValidacionUsuario reglas)
    {
        _reglas = reglas;
    }

    protected override void ValidarSolicitud(
        RegistrarParticipanteComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos;

        dto.DatosContacto ??= new DatosContactoDto();
        dto.DatosContacto.Telefono = _reglas.NormalizarTelefono(dto.DatosContacto.Telefono);

        _reglas.ValidarAlias(dto.Alias, resultado);
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
}
