using System.Text.RegularExpressions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

// HU03 — validador del registro público de Participante desde la app móvil.
//
// Las reglas comunes (correo, contraseña, datos personales, dirección,
// teléfono, fecha de nacimiento, sexo) se delegan en IReglasValidacionUsuario
// para que coincidan exactamente con las de HU02 sin duplicar mensajes ni
// expresiones regulares. La regla específica de este validador es el alias,
// que solo existe para Participante.
//
// El TipoUsuario no se valida porque el backend lo fija a Participante en el
// manejador — el cliente no puede pedir Operador / Administrador por esta
// vía. La detección de duplicados (alias / correo / nombre de usuario /
// teléfono) ocurre en el manejador.
public sealed class ValidadorRegistrarParticipante
    : ValidadorBase<RegistrarParticipanteComando>
{
    private static readonly Regex RegexAlias =
        new(@"^[a-zA-Z0-9._]+$", RegexOptions.Compiled);

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

        ValidarAlias(dto, resultado);
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

    private static void ValidarAlias(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.Alias))
        {
            r.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasObligatorio);
            return;
        }

        var valor = dto.Alias.Trim();
        if (valor.Length < 3 || valor.Length > 30)
        {
            r.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasLongitud);
            return;
        }
        if (!RegexAlias.IsMatch(valor))
            r.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasFormato);
    }
}
