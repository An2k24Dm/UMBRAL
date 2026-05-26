using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Mision
{
    public Guid Id { get; private set; }
    public Guid EtapaId { get; private set; }
    public string Titulo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public TipoMision Tipo { get; private set; }
    public string PistaClave { get; private set; } = default!;

    private Mision() { }

    internal static Mision Crear(
        Guid etapaId,
        string titulo,
        string descripcion,
        TipoMision tipo,
        string pistaClave)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ExcepcionDominio("El título de la misión es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la misión es obligatoria.");
        if (string.IsNullOrWhiteSpace(pistaClave))
            throw new ExcepcionDominio("La pista clave de la misión es obligatoria.");

        return new Mision
        {
            Id = Guid.NewGuid(),
            EtapaId = etapaId,
            Titulo = titulo.Trim(),
            Descripcion = descripcion.Trim(),
            Tipo = tipo,
            PistaClave = pistaClave.Trim()
        };
    }

    public static Mision Reconstituir(
        Guid id,
        Guid etapaId,
        string titulo,
        string descripcion,
        TipoMision tipo,
        string pistaClave)
    {
        return new Mision
        {
            Id = id,
            EtapaId = etapaId,
            Titulo = titulo,
            Descripcion = descripcion,
            Tipo = tipo,
            PistaClave = pistaClave
        };
    }
}
