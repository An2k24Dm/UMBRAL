using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Opcion
{
    public Guid Id { get; private set; }
    public Guid PreguntaId { get; private set; }
    public string Texto { get; private set; } = default!;
    public bool EsCorrecta { get; private set; }

    private Opcion() { }

    internal static Opcion Crear(Guid preguntaId, string texto, bool esCorrecta)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new ExcepcionDominio("El texto de la opción es obligatorio.");

        return new Opcion
        {
            Id = Guid.NewGuid(),
            PreguntaId = preguntaId,
            Texto = texto.Trim(),
            EsCorrecta = esCorrecta
        };
    }

    public static Opcion Reconstituir(Guid id, Guid preguntaId, string texto, bool esCorrecta)
    {
        return new Opcion
        {
            Id = id,
            PreguntaId = preguntaId,
            Texto = texto,
            EsCorrecta = esCorrecta
        };
    }
}
