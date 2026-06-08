using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Pregunta
{
    private readonly List<Opcion> _opciones = new();

    public Guid Id { get; private set; }
    public Guid TriviaId { get; private set; }
    public string Enunciado { get; private set; } = default!;
    public int PuntajeAsignado { get; private set; }
    public int TiempoEstimado { get; private set; }
    public IReadOnlyList<Opcion> Opciones => _opciones.AsReadOnly();

    private Pregunta() { }

    internal static Pregunta Crear(
        Guid triviaId,
        string enunciado,
        int puntaje,
        int tiempoEstimado,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones)
    {
        if (string.IsNullOrWhiteSpace(enunciado))
            throw new ExcepcionDominio("El enunciado de la pregunta es obligatorio.");
        if (puntaje <= 0 || puntaje % 5 != 0)
            throw new ExcepcionDominio("El puntaje debe ser un múltiplo de 5 (5, 10, 15… 100).");
        if (puntaje > 100)
            throw new ExcepcionDominio("El puntaje máximo por pregunta es 100.");
        if (tiempoEstimado <= 0)
            throw new ExcepcionDominio("El tiempo estimado debe ser mayor a cero.");

        var listaOpciones = opciones.ToList();
        if (listaOpciones.Count < 2)
            throw new ExcepcionDominio("La pregunta debe tener al menos dos opciones.");
        if (!listaOpciones.Any(o => o.EsCorrecta))
            throw new ExcepcionDominio("La pregunta debe tener exactamente una opción correcta.");
        if (listaOpciones.Count(o => o.EsCorrecta) > 1)
            throw new ExcepcionDominio("La pregunta solo puede tener una opción correcta.");

        var preguntaId = Guid.NewGuid();
        var pregunta = new Pregunta
        {
            Id = preguntaId,
            TriviaId = triviaId,
            Enunciado = enunciado.Trim(),
            PuntajeAsignado = puntaje,
            TiempoEstimado = tiempoEstimado
        };

        foreach (var (texto, esCorrecta) in listaOpciones)
            pregunta._opciones.Add(Opcion.Crear(preguntaId, texto, esCorrecta));

        return pregunta;
    }

    internal void Modificar(
        string nuevoEnunciado,
        int nuevoTiempoEstimado,
        IEnumerable<(string Texto, bool EsCorrecta)> nuevasOpciones)
    {
        if (string.IsNullOrWhiteSpace(nuevoEnunciado))
            throw new ExcepcionDominio("El enunciado de la pregunta es obligatorio.");
        if (nuevoTiempoEstimado <= 0)
            throw new ExcepcionDominio("El tiempo estimado debe ser mayor a cero.");

        var lista = nuevasOpciones.ToList();
        if (lista.Count < 2)
            throw new ExcepcionDominio("La pregunta debe tener al menos dos opciones.");
        if (!lista.Any(o => o.EsCorrecta))
            throw new ExcepcionDominio("La pregunta debe tener exactamente una opción correcta.");
        if (lista.Count(o => o.EsCorrecta) > 1)
            throw new ExcepcionDominio("La pregunta solo puede tener una opción correcta.");

        Enunciado = nuevoEnunciado.Trim();
        TiempoEstimado = nuevoTiempoEstimado;
        _opciones.Clear();
        foreach (var (texto, esCorrecta) in lista)
            _opciones.Add(Opcion.Crear(Id, texto, esCorrecta));
    }

    public static Pregunta Reconstituir(
        Guid id,
        Guid triviaId,
        string enunciado,
        int puntaje,
        int tiempoEstimado,
        IEnumerable<Opcion> opciones)
    {
        var pregunta = new Pregunta
        {
            Id = id,
            TriviaId = triviaId,
            Enunciado = enunciado,
            PuntajeAsignado = puntaje,
            TiempoEstimado = tiempoEstimado
        };
        pregunta._opciones.AddRange(opciones);
        return pregunta;
    }
}
