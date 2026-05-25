using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Trivia
{
    private readonly List<Pregunta> _preguntas = new();
    private readonly List<EventoDominio> _eventos = new();

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public int TiempoLimitePorPregunta { get; private set; }
    public EstadoTrivia Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public IReadOnlyList<Pregunta> Preguntas => _preguntas.AsReadOnly();
    public IReadOnlyList<EventoDominio> Eventos => _eventos.AsReadOnly();

    private Trivia() { }

    public static Trivia Crear(
        string nombre,
        string descripcion,
        Guid creadorId,
        int tiempoLimitePorPregunta,
        DateTime fechaCreacion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la trivia es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la trivia es obligatoria.");
        if (creadorId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del creador es obligatorio.");
        if (tiempoLimitePorPregunta <= 0)
            throw new ExcepcionDominio("El tiempo límite por pregunta debe ser mayor a cero.");

        var trivia = new Trivia
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            Descripcion = descripcion.Trim(),
            CreadorId = creadorId,
            TiempoLimitePorPregunta = tiempoLimitePorPregunta,
            Estado = EstadoTrivia.Borrador,
            FechaCreacion = fechaCreacion
        };

        trivia._eventos.Add(new TriviaCreadaEvento(trivia.Id, trivia.Nombre));
        return trivia;
    }

    public Pregunta AgregarPregunta(
        string enunciado,
        int puntaje,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones)
    {
        ValidarEstadoBorrador("agregar preguntas");

        var pregunta = Pregunta.Crear(Id, enunciado, puntaje, opciones);
        _preguntas.Add(pregunta);
        return pregunta;
    }

    public void ModificarPregunta(
        Guid preguntaId,
        string nuevoEnunciado,
        IEnumerable<(string Texto, bool EsCorrecta)> nuevasOpciones)
    {
        ValidarEstadoBorrador("modificar preguntas");

        var pregunta = _preguntas.FirstOrDefault(p => p.Id == preguntaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pregunta con ID '{preguntaId}'.");

        pregunta.Modificar(nuevoEnunciado, nuevasOpciones);
    }

    public void EliminarPregunta(Guid preguntaId)
    {
        ValidarEstadoBorrador("eliminar preguntas");

        var pregunta = _preguntas.FirstOrDefault(p => p.Id == preguntaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pregunta con ID '{preguntaId}'.");

        _preguntas.Remove(pregunta);
    }

    public void LimpiarEventos() => _eventos.Clear();

    public static Trivia Reconstituir(
        Guid id,
        string nombre,
        string descripcion,
        Guid creadorId,
        int tiempoLimitePorPregunta,
        EstadoTrivia estado,
        DateTime fechaCreacion,
        IEnumerable<Pregunta> preguntas)
    {
        var trivia = new Trivia
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            CreadorId = creadorId,
            TiempoLimitePorPregunta = tiempoLimitePorPregunta,
            Estado = estado,
            FechaCreacion = fechaCreacion
        };
        trivia._preguntas.AddRange(preguntas);
        return trivia;
    }

    private void ValidarEstadoBorrador(string accion)
    {
        if (Estado != EstadoTrivia.Borrador)
            throw new ExcepcionDominio(
                $"No se pueden {accion} a una trivia que no está en estado Borrador.");
    }
}
