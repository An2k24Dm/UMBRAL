using Microsoft.AspNetCore.Identity;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.Seguridad;

// Implementa el hasheo de la contraseña de un equipo privado usando
// PasswordHasher de ASP.NET Core Identity (PBKDF2 con sal aleatoria y
// múltiples iteraciones). No es SHA256 simple. El objeto de usuario es
// irrelevante para el algoritmo, por eso se usa un marcador.
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
