using IdentidadServicio.Aplicacion.Comandos.ModificarOperador;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorUnicidadModificarOperador
    : IValidadorAsincrono<ModificarOperadorComando>
{
    private readonly IRepositorioUnicidadUsuario _unicidad;

    public ValidadorUnicidadModificarOperador(IRepositorioUnicidadUsuario unicidad)
    {
        _unicidad = unicidad;
    }

    public async Task<ResultadoValidacion> ValidarAsync(
        ModificarOperadorComando comando,
        CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();
        var dto = comando.Datos;

        if (dto.NombreUsuario is not null &&
            await _unicidad.ExisteNombreUsuarioEnOtroUsuarioAsync(
                dto.NombreUsuario, comando.IdOperador, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioDuplicado);
        }

        if (dto.Correo is not null &&
            await _unicidad.ExisteCorreoEnOtroUsuarioAsync(
                dto.Correo, comando.IdOperador, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoDuplicado);
        }

        if (!string.IsNullOrWhiteSpace(dto.DatosContacto?.Telefono) &&
            await _unicidad.ExisteTelefonoEnOtroUsuarioAsync(
                dto.DatosContacto!.Telefono!, comando.IdOperador, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoDuplicado);
        }

        return resultado;
    }
}
