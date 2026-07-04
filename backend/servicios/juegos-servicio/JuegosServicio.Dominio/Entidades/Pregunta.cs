using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Pregunta
{
    private readonly List<Opcion> _opciones = new();
    public Guid Id { get; private set; }
    public Guid TriviaId { get; private set; }
    public string Enunciado { get; private set; } = default!;
    public Puntaje PuntajeAsignado { get; private set; } = default!;
    public Tiempo TiempoEstimado { get; private set; } = default!;
    public IReadOnlyList<Opcion> Opciones => _opciones.AsReadOnly();

    private Pregunta() { }

    internal static Pregunta Crear(
        Guid triviaId,
        string enunciado,
        Puntaje puntaje,
        Tiempo tiempoEstimado,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones)
    {
        if (string.IsNullOrWhiteSpace(enunciado))
            throw new ExcepcionDominio("El enunciado de la pregunta es obligatorio.");
        if (puntaje is null)
            throw new ExcepcionDominio("El puntaje de la pregunta es obligatorio.");
        if (tiempoEstimado is null)
            throw new ExcepcionDominio("El tiempo estimado de la pregunta es obligatorio.");

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
        Tiempo nuevoTiempoEstimado,
        IEnumerable<(string Texto, bool EsCorrecta)> nuevasOpciones)
    {
        if (string.IsNullOrWhiteSpace(nuevoEnunciado))
            throw new ExcepcionDominio("El enunciado de la pregunta es obligatorio.");
        if (nuevoTiempoEstimado is null)
            throw new ExcepcionDominio("El tiempo estimado de la pregunta es obligatorio.");

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
            PuntajeAsignado = Puntaje.DesdePersistencia(puntaje),
            TiempoEstimado = Tiempo.DesdePersistencia(tiempoEstimado)
        };
        pregunta._opciones.AddRange(opciones);
        return pregunta;
    }
}
