using System.Text.RegularExpressions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

// HU03 — validador del registro público de Participante desde la app móvil.
// Aplica las mismas reglas de formato/duplicados/edad que HU02 sobre los
// campos compartidos, pero sin la restricción de TipoUsuario (HU03 siempre
// es Participante) y añadiendo las reglas propias del alias.
public sealed class ValidadorRegistrarParticipante : IValidador<RegistrarParticipanteDto>
{
    private static readonly Regex RegexNombreUsuario =
        new(@"^[a-zA-Z0-9._]{4,30}$", RegexOptions.Compiled);

    private static readonly Regex RegexCorreo =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private static readonly Regex RegexTelefonoDigitos =
        new(@"^\d+$", RegexOptions.Compiled);

    private static readonly Regex RegexSoloLetras =
        new(@"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]+$", RegexOptions.Compiled);

    private static readonly Regex RegexAlias =
        new(@"^[a-zA-Z0-9._]+$", RegexOptions.Compiled);

    private const string CaracteresEspeciales = "!@#$%^&*_-.?";

    private static readonly string[] CodigosTelefonoValidos =
        { "0414", "0412", "0424", "0416", "0426", "0212" };

    private static readonly string[] SexosPermitidos =
        { "Masculino", "Femenino", "Otro", "Indefinido" };

    private readonly IRepositorioIdentidad _repositorio;
    private readonly IProveedorFechaHora _reloj;

    public ValidadorRegistrarParticipante(IRepositorioIdentidad repositorio, IProveedorFechaHora reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public async Task ValidarAsync(RegistrarParticipanteDto dto, CancellationToken cancelacion)
    {
        var r = new ResultadoValidacion();

        // Normaliza teléfono antes de validar para que el dominio reciba la
        // forma canónica (solo dígitos).
        dto.DatosContacto ??= new DatosContactoDto();
        dto.DatosContacto.Telefono = NormalizarTelefono(dto.DatosContacto.Telefono);

        ValidarAliasFormato(dto, r);
        ValidarNombreUsuarioFormato(dto, r);
        ValidarCorreoFormato(dto, r);
        ValidarContrasena(dto, r);
        ValidarNombre(dto, r);
        ValidarApellido(dto, r);
        ValidarTelefonoFormato(dto, r);
        ValidarDireccion(dto, r);
        ValidarFechaNacimiento(dto, r);
        ValidarSexo(dto, r);

        await ValidarDuplicadosAsync(dto, r, cancelacion);

        r.LanzarSiHayErrores();
    }

    private static string? NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;
        var limpio = new string(telefono.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
        return limpio.Length == 0 ? null : limpio;
    }

    private static void ValidarAliasFormato(RegistrarParticipanteDto dto, ResultadoValidacion r)
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

    private static void ValidarNombreUsuarioFormato(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreUsuario))
        {
            r.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioObligatorio);
            return;
        }
        if (!RegexNombreUsuario.IsMatch(dto.NombreUsuario.Trim()))
            r.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioFormato);
    }

    private static void ValidarCorreoFormato(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.Correo))
        {
            r.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoObligatorio);
            return;
        }
        if (!RegexCorreo.IsMatch(dto.Correo.Trim()))
            r.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoFormato);
    }

    private static void ValidarContrasena(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.Contrasena))
        {
            r.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaObligatoria);
            return;
        }
        var valor = dto.Contrasena;
        if (valor.Length < 5 || valor.Length > 10)
            r.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaLongitud);
        if (!valor.Any(char.IsDigit))
            r.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaSinNumero);
        if (!valor.Any(c => CaracteresEspeciales.Contains(c)))
            r.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaSinEspecial);
    }

    private static void ValidarNombre(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            r.Agregar(MensajesValidacionUsuario.CampoNombre,
                MensajesValidacionUsuario.NombreObligatorio);
            return;
        }
        var valor = dto.Nombre.Trim();
        if (valor.Length < 2 || valor.Length > 50)
            r.Agregar(MensajesValidacionUsuario.CampoNombre,
                MensajesValidacionUsuario.NombreLongitud);
        if (!RegexSoloLetras.IsMatch(valor))
            r.Agregar(MensajesValidacionUsuario.CampoNombre,
                MensajesValidacionUsuario.NombreSoloLetras);
    }

    private static void ValidarApellido(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.Apellido))
        {
            r.Agregar(MensajesValidacionUsuario.CampoApellido,
                MensajesValidacionUsuario.ApellidoObligatorio);
            return;
        }
        var valor = dto.Apellido.Trim();
        if (valor.Length < 2 || valor.Length > 50)
            r.Agregar(MensajesValidacionUsuario.CampoApellido,
                MensajesValidacionUsuario.ApellidoLongitud);
        if (!RegexSoloLetras.IsMatch(valor))
            r.Agregar(MensajesValidacionUsuario.CampoApellido,
                MensajesValidacionUsuario.ApellidoSoloLetras);
    }

    private static void ValidarTelefonoFormato(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        var telefono = dto.DatosContacto?.Telefono;
        if (string.IsNullOrWhiteSpace(telefono))
        {
            r.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoObligatorio);
            return;
        }
        if (!RegexTelefonoDigitos.IsMatch(telefono))
        {
            r.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoSoloNumeros);
            return;
        }
        if (telefono.Length != 11)
            r.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoLongitud);
        if (!CodigosTelefonoValidos.Any(c => telefono.StartsWith(c)))
            r.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoCodigoInvalido);
    }

    private static void ValidarDireccion(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        var direccion = dto.DatosContacto?.Direccion?.Trim();
        if (string.IsNullOrEmpty(direccion))
        {
            r.Agregar(MensajesValidacionUsuario.CampoDireccion,
                MensajesValidacionUsuario.DireccionObligatoria);
            return;
        }
        if (direccion.Length < 5)
            r.Agregar(MensajesValidacionUsuario.CampoDireccion,
                MensajesValidacionUsuario.DireccionLongitud);
    }

    private void ValidarFechaNacimiento(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (dto.FechaNacimiento == default)
        {
            r.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.FechaNacimientoObligatoria);
            return;
        }

        var ahora = _reloj.ObtenerFechaHoraUtc();
        if (dto.FechaNacimiento.Date > ahora.Date)
        {
            r.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.FechaNacimientoFutura);
            return;
        }

        var edad = CalcularEdad(dto.FechaNacimiento, ahora);
        if (edad < 18)
            r.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.EdadMinima);
        else if (edad > 100)
            r.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.EdadMaxima);
    }

    private static int CalcularEdad(DateTime nacimiento, DateTime ahora)
    {
        var edad = ahora.Year - nacimiento.Year;
        if (nacimiento.Date > ahora.AddYears(-edad).Date) edad--;
        return edad;
    }

    private static void ValidarSexo(RegistrarParticipanteDto dto, ResultadoValidacion r)
    {
        if (string.IsNullOrWhiteSpace(dto.Sexo)) return; // opcional → DtoMapeador resuelve Indefinido.
        if (!SexosPermitidos.Contains(dto.Sexo, StringComparer.OrdinalIgnoreCase))
            r.Agregar(MensajesValidacionUsuario.CampoSexo,
                MensajesValidacionUsuario.SexoInvalido);
    }

    private async Task ValidarDuplicadosAsync(
        RegistrarParticipanteDto dto, ResultadoValidacion r, CancellationToken cancelacion)
    {
        // Solo consultar duplicados si el valor tiene formato aceptable;
        // si ya hay error de formato, evitamos golpear la base.
        var revisaAlias = !r.Errores.Any(e => e.Campo == MensajesValidacionUsuario.CampoAlias);
        var revisaUsuario = !r.Errores.Any(e => e.Campo == MensajesValidacionUsuario.CampoNombreUsuario);
        var revisaCorreo = !r.Errores.Any(e => e.Campo == MensajesValidacionUsuario.CampoCorreo);
        var revisaTelefono = !r.Errores.Any(e => e.Campo == MensajesValidacionUsuario.CampoTelefono);

        if (revisaAlias &&
            await _repositorio.ExisteAliasAsync(dto.Alias, cancelacion))
        {
            r.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasDuplicado);
        }

        if (revisaUsuario &&
            await _repositorio.ExisteNombreUsuarioAsync(dto.NombreUsuario, cancelacion))
        {
            r.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioDuplicado);
        }

        if (revisaCorreo &&
            await _repositorio.ExisteCorreoAsync(dto.Correo, cancelacion))
        {
            r.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoDuplicado);
        }

        if (revisaTelefono && !string.IsNullOrWhiteSpace(dto.DatosContacto?.Telefono) &&
            await _repositorio.ExisteTelefonoAsync(dto.DatosContacto!.Telefono!, cancelacion))
        {
            r.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoDuplicado);
        }
    }
}
