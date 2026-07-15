namespace SesionesServicio.Aplicacion.Puertos;

public interface IHashContrasenaEquipo
{
    string Hashear(string contrasena);
    bool Verificar(string contrasena, string hash);
}
