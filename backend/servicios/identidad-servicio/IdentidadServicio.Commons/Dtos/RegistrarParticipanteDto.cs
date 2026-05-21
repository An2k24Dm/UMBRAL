namespace IdentidadServicio.Commons.Dtos;

// HU03 — registro público de Participante desde la app móvil.
// A diferencia de CrearUsuarioDto (HU02), este DTO no expone TipoUsuario: el
// backend asigna internamente RolUsuario.Participante para que el cliente no
// pueda intentar registrar Administrador u Operador por esta vía.
public sealed class RegistrarParticipanteDto
{
    // Alias visible en las experiencias del juego. Obligatorio y único.
    public string Alias { get; set; } = string.Empty;

    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    // Sexo viaja como string ("Masculino" | "Femenino" | "Indefinido" | "Otro")
    // y se convierte al enum del dominio dentro de la estrategia.
    public string Sexo { get; set; } = "Indefinido";

    public DateTime FechaNacimiento { get; set; }

    public DatosContactoDto DatosContacto { get; set; } = new();
}
