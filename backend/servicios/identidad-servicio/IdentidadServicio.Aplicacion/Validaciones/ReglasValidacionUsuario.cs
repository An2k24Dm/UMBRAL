using System.Text.RegularExpressions;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ReglasValidacionUsuario : IReglasValidacionUsuario
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
        new(@"^[a-zA-Z0-9_]{6,15}$", RegexOptions.Compiled);

    private const string CaracteresEspeciales = "!@#$%^&*_-.?";

    private static readonly string[] CodigosTelefonoValidos =
        { "0414", "0412", "0424", "0416", "0426", "0212" };

    private static readonly string[] SexosPermitidos =
        { "Masculino", "Femenino", "Otro", "Indefinido" };

    private readonly IProveedorFechaHora _reloj;

    public ReglasValidacionUsuario(IProveedorFechaHora reloj)
    {
        _reloj = reloj;
    }

    public void ValidarNombre(string? nombre, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoNombre,
                MensajesValidacionUsuario.NombreObligatorio);
            return;
        }
        var valor = nombre.Trim();
        if (valor.Length < 2 || valor.Length > 50)
            resultado.Agregar(MensajesValidacionUsuario.CampoNombre,
                MensajesValidacionUsuario.NombreLongitud);
        if (!RegexSoloLetras.IsMatch(valor))
            resultado.Agregar(MensajesValidacionUsuario.CampoNombre,
                MensajesValidacionUsuario.NombreSoloLetras);
    }

    public void ValidarApellido(string? apellido, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(apellido))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoApellido,
                MensajesValidacionUsuario.ApellidoObligatorio);
            return;
        }
        var valor = apellido.Trim();
        if (valor.Length < 2 || valor.Length > 50)
            resultado.Agregar(MensajesValidacionUsuario.CampoApellido,
                MensajesValidacionUsuario.ApellidoLongitud);
        if (!RegexSoloLetras.IsMatch(valor))
            resultado.Agregar(MensajesValidacionUsuario.CampoApellido,
                MensajesValidacionUsuario.ApellidoSoloLetras);
    }

    public void ValidarCorreo(string? correo, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoObligatorio);
            return;
        }
        if (!RegexCorreo.IsMatch(correo.Trim()))
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoFormato);
    }

    public void ValidarNombreUsuario(string? nombreUsuario, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioObligatorio);
            return;
        }
        if (!RegexNombreUsuario.IsMatch(nombreUsuario.Trim()))
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioFormato);
    }

    public void ValidarFechaNacimiento(DateTime? fechaNacimiento, ResultadoValidacion resultado)
    {
        if (fechaNacimiento is null || fechaNacimiento.Value == default)
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.FechaNacimientoObligatoria);
            return;
        }

        var ahora = _reloj.ObtenerFechaHoraUtc();
        var valor = fechaNacimiento.Value;
        if (valor.Date > ahora.Date)
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.FechaNacimientoFutura);
            return;
        }

        var edad = CalcularEdad(valor, ahora);
        if (edad < 18)
            resultado.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.EdadMinima);
        else if (edad > 100)
            resultado.Agregar(MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.EdadMaxima);
    }

    public void ValidarTelefono(string? telefono, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(telefono))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoObligatorio);
            return;
        }
        if (!RegexTelefonoDigitos.IsMatch(telefono))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoSoloNumeros);
            return;
        }
        if (telefono.Length != 11)
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoLongitud);
        if (!CodigosTelefonoValidos.Any(c => telefono.StartsWith(c)))
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoCodigoInvalido);
    }

    public void ValidarContrasena(string? contrasena, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaObligatoria);
            return;
        }
        if (contrasena.Length < 5 || contrasena.Length > 10)
            resultado.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaLongitud);
        if (!contrasena.Any(char.IsDigit))
            resultado.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaSinNumero);
        if (!contrasena.Any(c => CaracteresEspeciales.Contains(c)))
            resultado.Agregar(MensajesValidacionUsuario.CampoContrasena,
                MensajesValidacionUsuario.ContrasenaSinEspecial);
    }

    public void ValidarDireccion(string? direccion, ResultadoValidacion resultado)
    {
        var valor = direccion?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoDireccion,
                MensajesValidacionUsuario.DireccionObligatoria);
            return;
        }
        if (valor.Length < 5)
            resultado.Agregar(MensajesValidacionUsuario.CampoDireccion,
                MensajesValidacionUsuario.DireccionLongitud);
    }

    public void ValidarSexo(string? sexo, ResultadoValidacion resultado)
    {
        // Sexo es opcional en creación (HU02/HU03 → null/empty se mapea a
        // Indefinido en el DtoMapeador). Solo validamos si hay valor.
        if (string.IsNullOrWhiteSpace(sexo)) return;
        if (!SexosPermitidos.Contains(sexo, StringComparer.OrdinalIgnoreCase))
            resultado.Agregar(MensajesValidacionUsuario.CampoSexo,
                MensajesValidacionUsuario.SexoInvalido);
    }

    public void ValidarAlias(string? alias, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasObligatorio);
            return;
        }

        var valor = alias.Trim();

        // Longitud primero — el mensaje específico ayuda al cliente.
        if (valor.Length < 6 || valor.Length > 15)
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasLongitud);
            return;
        }

        // Si la longitud es válida pero contiene caracteres no permitidos,
        // reportamos el error de formato (no de longitud) para no confundir.
        if (!RegexAlias.IsMatch(valor))
            resultado.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasFormato);
    }

    public string? NormalizarTelefono(string? telefono)
    {
        if (telefono is null) return null;
        if (string.IsNullOrWhiteSpace(telefono)) return string.Empty;
        return new string(telefono.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
    }

    private static int CalcularEdad(DateTime nacimiento, DateTime ahora)
    {
        var edad = ahora.Year - nacimiento.Year;
        if (nacimiento.Date > ahora.AddYears(-edad).Date) edad--;
        return edad;
    }
}
