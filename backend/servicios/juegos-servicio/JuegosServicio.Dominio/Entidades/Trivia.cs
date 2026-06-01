using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Estados;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Trivia
{
    private readonly List<Pregunta> _preguntas = new();
    private readonly List<EventoDominio> _eventos = new();
    private IEstadoTrivia _estado = default!;

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
            Estado = EstadoTrivia.Inactiva,
            FechaCreacion = fechaCreacion
        };
        trivia._estado = FabricaEstadoTrivia.Obtener(EstadoTrivia.Inactiva);

        trivia._eventos.Add(new TriviaCreadaEvento(trivia.Id, trivia.Nombre));
        return trivia;
    }

    public Pregunta AgregarPregunta(
        string enunciado,
        int puntaje,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones)
    {
        _estado.ValidarEdicion("agregar preguntas");

        if (_preguntas.Count >= 20)
            throw new ExcepcionDominio("La trivia no puede tener más de 20 preguntas.");

        var pregunta = Pregunta.Crear(Id, enunciado, puntaje, opciones);
        _preguntas.Add(pregunta);
        return pregunta;
    }

    public void ModificarPregunta(
        Guid preguntaId,
        string nuevoEnunciado,
        IEnumerable<(string Texto, bool EsCorrecta)> nuevasOpciones)
    {
        _estado.ValidarEdicion("modificar preguntas");

        var pregunta = _preguntas.FirstOrDefault(p => p.Id == preguntaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pregunta con ID '{preguntaId}'.");

        pregunta.Modificar(nuevoEnunciado, nuevasOpciones);
    }

    public void EliminarPregunta(Guid preguntaId)
    {
        _estado.ValidarEdicion("eliminar preguntas");

        var pregunta = _preguntas.FirstOrDefault(p => p.Id == preguntaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pregunta con ID '{preguntaId}'.");

        _preguntas.Remove(pregunta);
    }

    // Patrón State: delega en el objeto de estado actual.
    public void Activar() => _estado.Activar(this);
    public void Desactivar() => _estado.Desactivar(this);

    public void ModificarDatos(string nuevoNombre, string nuevaDescripcion, int nuevoTiempo)
    {
        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ExcepcionDominio("El nombre de la trivia es obligatorio.");
        if (string.IsNullOrWhiteSpace(nuevaDescripcion))
            throw new ExcepcionDominio("La descripción de la trivia es obligatoria.");
        if (nuevoTiempo <= 0)
            throw new ExcepcionDominio("El tiempo límite por pregunta debe ser mayor a cero.");

        Nombre = nuevoNombre.Trim();
        Descripcion = nuevaDescripcion.Trim();
        TiempoLimitePorPregunta = nuevoTiempo;
        _eventos.Add(new TriviaModificadaEvento(Id, Nombre, TiempoLimitePorPregunta));
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
        trivia._estado = FabricaEstadoTrivia.Obtener(estado);
        trivia._preguntas.AddRange(preguntas);
        return trivia;
    }

    // Métodos internos para uso exclusivo de los estados (patrón State).
    internal void TransicionarEstado(EstadoTrivia nuevoEstado)
    {
        Estado = nuevoEstado;
        _estado = FabricaEstadoTrivia.Obtener(nuevoEstado);
    }

    internal void AgregarEventoInterno(EventoDominio evento) => _eventos.Add(evento);
}
