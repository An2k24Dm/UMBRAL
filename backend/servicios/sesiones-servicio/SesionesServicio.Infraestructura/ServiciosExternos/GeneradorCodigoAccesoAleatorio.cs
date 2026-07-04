using System.Security.Cryptography;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class GeneradorCodigoAccesoAleatorio : IGeneradorCodigoAcceso
{
    private const string AlfabetoSinAmbiguos = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Longitud = 6;

    public string Generar()
    {
        Span<byte> buffer = stackalloc byte[Longitud];
        RandomNumberGenerator.Fill(buffer);

        Span<char> caracteres = stackalloc char[Longitud];
        for (var i = 0; i < Longitud; i++)
        {
            var indice = buffer[i] % AlfabetoSinAmbiguos.Length;
            caracteres[i] = AlfabetoSinAmbiguos[indice];
        }
        return new string(caracteres);
    }
}
