using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Infraestructura.Persistencia.Modelos;

namespace JuegosServicio.Infraestructura.Persistencia;

public static class BusquedasMapeador
{
    // Dominio → Modelos

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
            Etapas = busqueda.Etapas.Select(AModelo).ToList()
        };
    }

    public static EtapaModelo AModelo(Etapa etapa)
    {
        return new EtapaModelo
        {
            Id = etapa.Id,
            BusquedaId = etapa.BusquedaId,
            Titulo = etapa.Titulo,
            Descripcion = etapa.Descripcion,
            Orden = etapa.Orden,
            Misiones = etapa.Misiones.Select(AModelo).ToList(),
            Pistas = etapa.Pistas.Select(AModelo).ToList()
        };
    }

    public static MisionModelo AModelo(Mision mision)
    {
        return new MisionModelo
        {
            Id = mision.Id,
            EtapaId = mision.EtapaId,
            Titulo = mision.Titulo,
            Descripcion = mision.Descripcion,
            Tipo = (int)mision.Tipo,
            PistaClave = mision.PistaClave
        };
    }

    public static PistaModelo AModelo(Pista pista)
    {
        return new PistaModelo
        {
            Id = pista.Id,
            EtapaId = pista.EtapaId,
            Contenido = pista.Contenido
        };
    }

    // Modelos → Dominio

    public static BusquedaTesoro ADominio(BusquedaTesoroModelo modelo)
    {
        var etapas = modelo.Etapas.Select(ADominio);
        return BusquedaTesoro.Reconstituir(
            modelo.Id,
            modelo.Nombre,
            modelo.Descripcion,
            modelo.CreadorId,
            (EstadoBusqueda)modelo.Estado,
            modelo.FechaCreacion,
            etapas);
    }

    public static Etapa ADominio(EtapaModelo modelo)
    {
        var misiones = modelo.Misiones.Select(ADominio);
        var pistas = modelo.Pistas.Select(ADominio);
        return Etapa.Reconstituir(
            modelo.Id,
            modelo.BusquedaId,
            modelo.Titulo,
            modelo.Descripcion,
            modelo.Orden,
            misiones,
            pistas);
    }

    public static Mision ADominio(MisionModelo modelo)
    {
        return Mision.Reconstituir(
            modelo.Id,
            modelo.EtapaId,
            modelo.Titulo,
            modelo.Descripcion,
            (TipoMision)modelo.Tipo,
            modelo.PistaClave);
    }

    public static Pista ADominio(PistaModelo modelo)
    {
        return Pista.Reconstituir(modelo.Id, modelo.EtapaId, modelo.Contenido);
    }
}
