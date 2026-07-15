using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Infraestructura.Persistencia.Modelos;

namespace JuegosServicio.Infraestructura.Persistencia.Mapeadores;

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
            Tiempo = busqueda.Tiempo.Valor,
            Puntaje = busqueda.Puntaje.Valor,
            CodigoQr = busqueda.CodigoQr,
            Pistas = busqueda.Pistas.Select(AModelo).ToList()
        };
    }

    public static PistaModelo AModelo(Pista pista)
    {
        return new PistaModelo
        {
            Id = pista.Id,
            BusquedaId = pista.BusquedaId,
            Contenido = pista.Contenido,
            Tipo = (int)pista.Tipo,
            Latitud = pista.Latitud,
            Longitud = pista.Longitud
        };
    }

    public static BusquedaTesoro ADominio(BusquedaTesoroModelo modelo)
    {
        var pistas = modelo.Pistas.Select(ADominio);
        return BusquedaTesoro.Reconstituir(
            modelo.Id,
            modelo.Nombre,
            modelo.Descripcion,
            modelo.CreadorId,
            (EstadoBusqueda)modelo.Estado,
            modelo.FechaCreacion,
            modelo.Tiempo,
            modelo.Puntaje,
            pistas,
            modelo.CodigoQr);
    }

    public static Pista ADominio(PistaModelo modelo) =>
        Pista.Reconstituir(modelo.Id, modelo.BusquedaId, modelo.Contenido,
            (TipoPista)modelo.Tipo, modelo.Latitud, modelo.Longitud);
}
