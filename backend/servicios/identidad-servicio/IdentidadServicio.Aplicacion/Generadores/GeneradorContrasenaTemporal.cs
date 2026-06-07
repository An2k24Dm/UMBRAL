using System.Security.Cryptography;
using System.Text;

namespace IdentidadServicio.Aplicacion.Generadores;

public sealed class GeneradorContrasenaTemporal : IGeneradorContrasenaTemporal
{
    private const int LongitudTotal = 14;
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Digitos = "23456789";
    // Caracteres especiales seguros para copiar desde un correo y para
    // transportar en JSON sin escapados confusos. Se excluyen explícitamente:
    //   espacios, comillas (' " `), < > & (HTML-sensibles que el encoder JSON
    //   convierte en \uXXXX y que algunos clientes de correo o navegadores
    //   re-codifican), \ / | (barras y diagonales), $ ^ (ambiguos en algunos
    //   shells), y cualquier salto de línea o tabulación.
    private const string Especiales = "!@#%*_-.?";
    private const string Todos = Mayusculas + Minusculas + Digitos + Especiales;

    public string Generar()
    {
        var caracteres = new char[LongitudTotal];

        caracteres[0] = ElegirAleatorio(Mayusculas);
        caracteres[1] = ElegirAleatorio(Minusculas);
        caracteres[2] = ElegirAleatorio(Digitos);
        caracteres[3] = ElegirAleatorio(Especiales);

        for (var i = 4; i < LongitudTotal; i++)
            caracteres[i] = ElegirAleatorio(Todos);

        BarajarSeguro(caracteres);

        return new string(caracteres);
    }

    private static char ElegirAleatorio(string fuente) =>
        fuente[RandomNumberGenerator.GetInt32(fuente.Length)];

    private static void BarajarSeguro(char[] datos)
    {
        for (var i = datos.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (datos[i], datos[j]) = (datos[j], datos[i]);
        }
    }
}
