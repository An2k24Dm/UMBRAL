namespace SesionesServicio.Aplicacion.Puertos;

// Puerto de aplicación para proteger la contraseña de un equipo privado.
// La implementación usa un algoritmo con sal (no SHA256 simple). En HU40
// solo se usa Hashear; Verificar queda disponible para HU47 (ingreso a
// equipo privado).
public interface IHashContrasenaEquipo
{
    string Hashear(string contrasena);
    bool Verificar(string contrasena, string hash);
}
