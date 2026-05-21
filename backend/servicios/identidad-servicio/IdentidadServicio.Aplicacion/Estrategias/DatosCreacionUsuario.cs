using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

// Modelo interno de aplicación que las estrategias consumen para crear un
// agregado de usuario. Vive en la capa de Aplicación y no es contrato público
// de la API: cada caso de uso (HU02, HU03, ...) mapea su DTO de entrada hacia
// este modelo para que las estrategias no dependan directamente del DTO de
// transporte.
//
// El campo Alias solo aplica para RolUsuario.Participante (HU03). Para
// Administrador/Operador (HU02) llega como null.
public sealed class DatosCreacionUsuario
{
    public RolUsuario TipoUsuario { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Sexo { get; init; } = "Indefinido";
    public DateTime FechaNacimiento { get; init; }
    public DatosContactoDto DatosContacto { get; init; } = new();
    public string? Alias { get; init; }

    // FechaNacimiento llega desde el cliente como string YYYY-MM-DD y ASP.NET
    // la deserializa como DateTimeKind.Unspecified. PostgreSQL (Npgsql) exige
    // DateTimeKind.Utc para columnas timestamptz, así que la normalizamos a
    // UTC manteniendo solo la parte de la fecha (sin hora) antes de cruzar a
    // la capa de dominio/persistencia. Esto no usa IProveedorFechaHora porque
    // no es la fecha "actual": es un dato que aporta el usuario.
    public static DateTime NormalizarFechaNacimiento(DateTime fecha) =>
        DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
}
