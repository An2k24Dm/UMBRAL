using Mapster;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.Infraestructura.Mapeadores;

// Punto de extensión Mapster. Hoy el mapeo principal (Sesion ↔
// SesionModelo) se hace mediante SesionesMapeador, pero dejamos esta
// configuración global registrada para que historias posteriores que
// necesiten proyecciones automáticas (informes, exportaciones, etc.)
// puedan engancharse aquí sin tocar la persistencia.
public static class ConfiguracionMapsterSesiones
{
    public static void Configurar(TypeAdapterConfig config)
    {
        config.NewConfig<Sesion, SesionModelo>();
        config.NewConfig<SesionModelo, Sesion>()
            .ConstructUsing(m => Sesion.Rehidratar(
                m.Id,
                m.Nombre,
                m.TipoJuego,
                m.ContenidoJuegoId,
                m.Modo,
                m.Estado,
                m.FechaProgramada,
                m.CreadaPorUsuarioId,
                m.FechaCreacion));
    }
}
