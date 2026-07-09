using Mapster;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

public static class ConfiguracionMapsterSesiones
{
    public static void Configurar(TypeAdapterConfig config)
    {
        config.NewConfig<Sesion, SesionModelo>();
    }
}
