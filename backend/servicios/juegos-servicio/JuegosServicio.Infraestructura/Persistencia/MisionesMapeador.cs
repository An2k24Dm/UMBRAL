using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Infraestructura.Persistencia.Modelos;

namespace JuegosServicio.Infraestructura.Persistencia;

public static class MisionesMapeador
{
    public static MisionModelo AModelo(Mision mision)
    {
        return new MisionModelo
        {
            Id = mision.Id,
            Nombre = mision.Nombre,
            Descripcion = mision.Descripcion,
            CreadorId = mision.CreadorId,
            Estado = (int)mision.Estado,
            Dificultad = (int)mision.Dificultad,
            FechaCreacion = mision.FechaCreacion,
            Etapas = mision.Etapas.Select(AModelo).ToList()
        };
    }

    public static EtapaModelo AModelo(Etapa etapa)
    {
        return new EtapaModelo
        {
            Id = etapa.Id,
            MisionId = etapa.MisionId,
            Orden = etapa.Orden,
            TipoModoDeJuego = (int)etapa.TipoModoDeJuego,
            ModoDeJuegoId = etapa.ModoDeJuegoId
        };
    }

    public static Mision ADominio(MisionModelo modelo)
    {
        var etapas = modelo.Etapas.Select(ADominio);
        return Mision.Reconstituir(
            modelo.Id,
            modelo.Nombre,
            modelo.Descripcion,
            modelo.CreadorId,
            (EstadoMision)modelo.Estado,
            (NivelDificultad)modelo.Dificultad,
            modelo.FechaCreacion,
            etapas);
    }

    public static Etapa ADominio(EtapaModelo modelo) =>
        Etapa.Reconstituir(
            modelo.Id,
            modelo.MisionId,
            modelo.Orden,
            (TipoModoDeJuego)modelo.TipoModoDeJuego,
            modelo.ModoDeJuegoId);
}
