using Mapster;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

// Punto de extensiÃ³n Mapster reservado. Hoy el mapeo dominioâ†”persistencia
// lo hacen las estrategias MapeadorSesionesPersistencia manualmente; esta
// config queda registrada por si historias futuras agregan proyecciones
// automÃ¡ticas.
public static class ConfiguracionMapsterSesiones
{
    public static void Configurar(TypeAdapterConfig config)
    {
        config.NewConfig<Sesion, SesionModelo>();
    }
}
