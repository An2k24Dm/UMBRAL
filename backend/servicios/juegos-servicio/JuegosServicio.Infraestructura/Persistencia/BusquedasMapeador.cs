using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Infraestructura.Persistencia.Modelos;

namespace JuegosServicio.Infraestructura.Persistencia;

public static class BusquedasMapeador
{
    public static BusquedaTesoroModelo AModelo(BusquedaTesoro busqueda)
    {
        return new BusquedaTesoroModelo
        {
            Id = busqueda.Id,
            Nombre = busqueda.Nombre,
            Descripcion = busqueda.Descripcion,
            CreadorId = busqueda.CreadorId,
            Estado = (int)busqueda.Estado,
            FechaCreacion = busqueda.FechaCreacion,
            Mision = busqueda.Mision is null ? null : AModelo(busqueda.Mision)
        };
    }

    public static MisionModelo AModelo(Mision mision)
    {
        return new MisionModelo
        {
            Id = mision.Id,
            BusquedaId = mision.BusquedaId,
            Titulo = mision.Titulo,
            Descripcion = mision.Descripcion,
            Tipo = (int)mision.Tipo,
            PistaClave = mision.PistaClave,
            Pistas = mision.Pistas.Select(AModelo).ToList()
        };
    }

    public static PistaModelo AModelo(Pista pista)
    {
        return new PistaModelo
        {
            Id = pista.Id,
            MisionId = pista.MisionId,
            Contenido = pista.Contenido
        };
    }

    public static BusquedaTesoro ADominio(BusquedaTesoroModelo modelo)
    {
        var mision = modelo.Mision is null ? null : ADominio(modelo.Mision);
        return BusquedaTesoro.Reconstituir(
            modelo.Id,
            modelo.Nombre,
            modelo.Descripcion,
            modelo.CreadorId,
            (EstadoBusqueda)modelo.Estado,
            modelo.FechaCreacion,
            mision);
    }

    public static Mision ADominio(MisionModelo modelo)
    {
        var pistas = modelo.Pistas.Select(ADominio);
        return Mision.Reconstituir(
            modelo.Id,
            modelo.BusquedaId,
            modelo.Titulo,
            modelo.Descripcion,
            (TipoMision)modelo.Tipo,
            modelo.PistaClave,
            pistas);
    }

    public static Pista ADominio(PistaModelo modelo)
    {
        return Pista.Reconstituir(modelo.Id, modelo.MisionId, modelo.Contenido);
    }
}
