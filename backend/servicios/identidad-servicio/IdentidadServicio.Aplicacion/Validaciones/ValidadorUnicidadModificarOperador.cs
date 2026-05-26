using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

// HU09 — validador asíncrono de unicidad para la edición de Operador.
//
// Reemplaza al método privado ValidarDuplicadosAsync que vivía dentro del
// manejador. Se aísla aquí porque:
//  1. Es lógica genuinamente de validación (rechaza la solicitud si choca con
//     otro usuario), no de coordinación del caso de uso.
//  2. Necesita un puerto asincrónico (IRepositorioUnicidadUsuario), por lo
//     que no encaja en IValidador<T> (sincrónico) ni en IReglasValidacionUsuario.
//  3. Mantener el manejador como simple coordinador respeta SRP.
//
// Las consultas EXCLUYEN al propio Operador para que el cliente pueda
// reenviar valores ya asignados al mismo usuario sin que se interpreten
// como colisión.
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
