namespace SesionesServicio.Commons.Dtos;

// HU47 — Cuerpo del POST /api/sesiones/{sesionId}/equipos/{equipoId}/ingresar.
// La contraseña solo es necesaria para equipos privados; en públicos se
// ignora. El participante se resuelve del JWT, nunca del body.
public sealed class IngresarEquipoDto
{
    public string? Contrasena { get; set; }
}
