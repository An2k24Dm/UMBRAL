using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorUnicidadUsuario
{
    private readonly IRepositorioUnicidadUsuario _unicidad;

    public ValidadorUnicidadUsuario(IRepositorioUnicidadUsuario unicidad)
    {
        _unicidad = unicidad;
    }

    public async Task ValidarCreacionUsuarioAsync(
        CrearUsuarioDto dto, CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();

        await ValidarCamposComunesAsync(
            dto.NombreUsuario, dto.Correo, dto.DatosContacto?.Telefono, resultado, cancelacion);

        resultado.LanzarSiHayErrores();
    }

    public async Task ValidarRegistroParticipanteAsync(
        RegistrarParticipanteDto dto, CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();

        if (await _unicidad.ExisteAliasAsync(dto.Alias, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasDuplicado);

        await ValidarCamposComunesAsync(
            dto.NombreUsuario, dto.Correo, dto.DatosContacto?.Telefono, resultado, cancelacion);

        resultado.LanzarSiHayErrores();
    }

    private async Task ValidarCamposComunesAsync(
        string nombreUsuario,
        string correo,
        string? telefono,
        ResultadoValidacion resultado,
        CancellationToken cancelacion)
    {
        if (await _unicidad.ExisteNombreUsuarioAsync(nombreUsuario, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioDuplicado);

        if (await _unicidad.ExisteCorreoAsync(correo, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoDuplicado);

        if (!string.IsNullOrWhiteSpace(telefono) &&
            await _unicidad.ExisteTelefonoAsync(telefono!, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoDuplicado);
    }
}
