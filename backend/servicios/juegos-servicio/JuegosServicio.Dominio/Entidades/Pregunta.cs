using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Pregunta
{
    private readonly List<Opcion> _opciones = new();

    public Guid Id { get; private set; }
    public Guid TriviaId { get; private set; }
    public string Enunciado { get; private set; } = default!;
    public int PuntajeAsignado { get; private set; }
    public IReadOnlyList<Opcion> Opciones => _opciones.AsReadOnly();

    private Pregunta() { }

    internal static Pregunta Crear(
        Guid triviaId,
        string enunciado,
        int puntaje,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones)
    {
        if (string.IsNullOrWhiteSpace(enunciado))
            throw new ExcepcionDominio("El enunciado de la pregunta es obligatorio.");
        if (puntaje <= 0)
            throw new ExcepcionDominio("El puntaje asignado debe ser mayor a cero.");

        var listaOpciones = opciones.ToList();
        if (listaOpciones.Count < 2)
            throw new ExcepcionDominio("La pregunta debe tener al menos dos opciones.");
        if (!listaOpciones.Any(o => o.EsCorrecta))
            throw new ExcepcionDominio("Al menos una opción debe estar marcada como correcta.");

        var preguntaId = Guid.NewGuid();
        var pregunta = new Pregunta
        {
            Id = preguntaId,
            TriviaId = triviaId,
            Enunciado = enunciado.Trim(),
            PuntajeAsignado = puntaje
        };

        foreach (var (texto, esCorrecta) in listaOpciones)
            pregunta._opciones.Add(Opcion.Crear(preguntaId, texto, esCorrecta));

        return pregunta;
    }

    internal void Modificar(string nuevoEnunciado, IEnumerable<(string Texto, bool EsCorrecta)> nuevasOpciones)
    {
        if (string.IsNullOrWhiteSpace(nuevoEnunciado))
            throw new ExcepcionDominio("El enunciado de la pregunta es obligatorio.");

        var lista = nuevasOpciones.ToList();
        if (lista.Count < 2)
            throw new ExcepcionDominio("La pregunta debe tener al menos dos opciones.");
        if (!lista.Any(o => o.EsCorrecta))
            throw new ExcepcionDominio("Al menos una opción debe estar marcada como correcta.");

        Enunciado = nuevoEnunciado.Trim();
        _opciones.Clear();
        foreach (var (texto, esCorrecta) in lista)
            _opciones.Add(Opcion.Crear(Id, texto, esCorrecta));
    }

    public static Pregunta Reconstituir(
        Guid id,
        Guid triviaId,
        string enunciado,
        int puntaje,
        IEnumerable<Opcion> opciones)
    {
        var pregunta = new Pregunta
        {
            Id = id,
            TriviaId = triviaId,
            Enunciado = enunciado,
            PuntajeAsignado = puntaje
        };
        pregunta._opciones.AddRange(opciones);
        return pregunta;
    }
}
