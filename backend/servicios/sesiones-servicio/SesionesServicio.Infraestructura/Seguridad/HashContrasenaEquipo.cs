using Microsoft.AspNetCore.Identity;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.Seguridad;

public sealed class HashContrasenaEquipo : IHashContrasenaEquipo
{
    private static readonly object Marcador = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hashear(string contrasena)
        => _hasher.HashPassword(Marcador, contrasena);

    public bool Verificar(string contrasena, string hash)
    {
        var resultado = _hasher.VerifyHashedPassword(Marcador, hash, contrasena);
        return resultado != PasswordVerificationResult.Failed;
    }
}
