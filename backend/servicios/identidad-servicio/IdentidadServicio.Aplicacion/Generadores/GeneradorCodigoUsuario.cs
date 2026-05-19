using System.Text.RegularExpressions;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Generadores;

public sealed class GeneradorCodigoUsuario : IGeneradorCodigoUsuario
{
    private const string PrefijoOperador = "OP-";
    private const string PrefijoAdministrador = "AD-";

    private static readonly Regex PatronOperador =
        new(@"^OP-(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PatronAdministrador =
        new(@"^AD-(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IRepositorioIdentidad _repositorio;

    public GeneradorCodigoUsuario(IRepositorioIdentidad repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<string> GenerarCodigoOperadorAsync(CancellationToken cancelacion)
    {
        var ultimo = await _repositorio.ObtenerUltimoCodigoOperadorAsync(cancelacion);
        return Formatear(PrefijoOperador, SiguienteNumero(ultimo, PatronOperador));
    }

    public async Task<string> GenerarCodigoAdministradorAsync(CancellationToken cancelacion)
    {
        var ultimo = await _repositorio.ObtenerUltimoCodigoAdministradorAsync(cancelacion);
        return Formatear(PrefijoAdministrador, SiguienteNumero(ultimo, PatronAdministrador));
    }

    private static int SiguienteNumero(string? ultimoCodigo, Regex patron)
    {
        if (string.IsNullOrWhiteSpace(ultimoCodigo)) return 1;

        var coincidencia = patron.Match(ultimoCodigo.Trim());
        if (!coincidencia.Success) return 1;

        return int.TryParse(coincidencia.Groups[1].Value, out var n) ? n + 1 : 1;
    }

    private static string Formatear(string prefijo, int numero) =>
        $"{prefijo}{numero:D3}";
}
