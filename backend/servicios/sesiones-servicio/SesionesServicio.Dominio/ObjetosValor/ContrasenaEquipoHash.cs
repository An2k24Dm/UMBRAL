using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.ObjetosValor;

// Value Object que representa el HASH de la contraseña de un equipo privado.
// Nunca contiene ni reconstruye la contraseña en texto plano: el hasheo
// ocurre en la capa de aplicación/infraestructura antes de entrar al
// agregado. Igualdad por valor sobre el hash.
public sealed class ContrasenaEquipoHash : IEquatable<ContrasenaEquipoHash>
{
    public string Valor { get; }

    private ContrasenaEquipoHash(string valor)
    {
        Valor = valor;
    }

    public static ContrasenaEquipoHash Crear(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new EquipoInvalidoExcepcion(
                "El hash de la contraseña del equipo es obligatorio.");

        return new ContrasenaEquipoHash(hash.Trim());
    }

    public bool Equals(ContrasenaEquipoHash? otro)
        => otro is not null && string.Equals(Valor, otro.Valor, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ContrasenaEquipoHash);

    public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);

    // No expone la contraseña original; ToString tampoco revela el hash.
    public override string ToString() => "********";
}
