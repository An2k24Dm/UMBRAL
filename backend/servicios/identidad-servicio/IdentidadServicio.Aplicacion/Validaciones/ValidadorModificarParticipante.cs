using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarParticipante
    : ValidadorBase<ModificarParticipanteComando>
{
    private readonly IReglasValidacionUsuario _reglas;

    public ValidadorModificarParticipante(IReglasValidacionUsuario reglas)
    {
        _reglas = reglas;
    }

    protected override void ValidarSolicitud(
        ModificarParticipanteComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos;

        ValidadorReglasModificacionPerfilUsuario.Validar(
            dto, _reglas, resultado);

        if (dto.Alias is not null)
            _reglas.ValidarAlias(dto.Alias, resultado);

        ValidarCambioContrasenaParticipante(dto, resultado);
    }

    private void ValidarCambioContrasenaParticipante(
        Commons.Dtos.ModificarParticipanteSolicitudDto dto,
        ResultadoValidacion resultado)
    {
        var solicita = dto.NuevaContrasena is not null || dto.ConfirmacionContrasena is not null;
        if (!solicita) return;

        _reglas.ValidarContrasena(dto.NuevaContrasena, resultado);

        if (!string.Equals(dto.NuevaContrasena, dto.ConfirmacionContrasena, StringComparison.Ordinal))
        {
            resultado.Agregar(
                MensajesValidacionUsuario.CampoConfirmacionContrasena,
                MensajesValidacionUsuario.ContrasenasNoCoinciden);
        }
    }
}
