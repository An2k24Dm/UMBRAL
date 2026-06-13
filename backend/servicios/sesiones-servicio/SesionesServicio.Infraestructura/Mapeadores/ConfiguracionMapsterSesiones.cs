using Mapster;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.Infraestructura.Mapeadores;

// Punto de extensión Mapster reservado. Hoy el mapeo dominio↔persistencia
// lo hacen las estrategias MapeadorSesionesPersistencia manualmente; esta
// config queda registrada por si historias futuras agregan proyecciones
// automáticas.
public static class ConfiguracionMapsterSesiones
{
    public static void Configurar(TypeAdapterConfig config)
    {
        config.NewConfig<Sesion, SesionModelo>();
    }
}
