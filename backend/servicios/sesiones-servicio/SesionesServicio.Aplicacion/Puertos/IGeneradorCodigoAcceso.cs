namespace SesionesServicio.Aplicacion.Puertos;

// Genera el código de acceso con el que un Participante encuentra una
// sesión desde la app móvil. La implementación vive en Infraestructura
// para que la capa de Aplicación pueda ser probada con un doble que
// devuelva un código determinístico.
public interface IGeneradorCodigoAcceso
{
    string Generar();
}
