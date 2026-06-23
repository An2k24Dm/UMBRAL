using IdentidadServicio.Aplicacion.Comandos.ModificarParticipante;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorUnicidadModificarParticipante
    : IValidadorAsincrono<ModificarParticipanteComando>
{
    private readonly IRepositorioUnicidadUsuario _unicidad;

    public ValidadorUnicidadModificarParticipante(IRepositorioUnicidadUsuario unicidad)
    {
        _unicidad = unicidad;
    }

    public async Task<ResultadoValidacion> ValidarAsync(
        ModificarParticipanteComando comando, CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();
        var dto = comando.Datos;
        var idActual = comando.IdParticipanteActual;

        if (idActual == Guid.Empty) return resultado;

        if (dto.NombreUsuario is not null &&
            await _unicidad.ExisteNombreUsuarioEnOtroUsuarioAsync(
                dto.NombreUsuario, idActual, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioDuplicado);
        }

        if (dto.Correo is not null &&
            await _unicidad.ExisteCorreoEnOtroUsuarioAsync(
                dto.Correo, idActual, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoDuplicado);
        }

        if (!string.IsNullOrWhiteSpace(dto.DatosContacto?.Telefono) &&
            await _unicidad.ExisteTelefonoEnOtroUsuarioAsync(
                dto.DatosContacto!.Telefono!, idActual, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoDuplicado);
        }

        if (!string.IsNullOrWhiteSpace(dto.Alias) &&
            await _unicidad.ExisteAliasEnOtroUsuarioAsync(
                dto.Alias!, idActual, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasDuplicado);
        }

        return resultado;
    }
}
