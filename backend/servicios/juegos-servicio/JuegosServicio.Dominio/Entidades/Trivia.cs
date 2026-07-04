using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Trivia : IComponenteJuego
{
    private readonly List<Pregunta> _preguntas = new();
    private readonly List<EventoDominio> _eventos = new();
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public Tiempo TiempoLimitePorPregunta { get; private set; } = default!;
    public EstadoTrivia Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public IReadOnlyList<Pregunta> Preguntas => _preguntas.AsReadOnly();
    public IReadOnlyList<EventoDominio> Eventos => _eventos.AsReadOnly();

    private Trivia() { }

    public static Trivia Crear(
        string nombre,
        string descripcion,
        Guid creadorId,
        Tiempo tiempoLimitePorPregunta,
        DateTime fechaCreacion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la trivia es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la trivia es obligatoria.");
        if (creadorId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del creador es obligatorio.");
        if (tiempoLimitePorPregunta is null)
            throw new ExcepcionDominio("El tiempo límite por pregunta es obligatorio.");

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
        trivia._eventos.Add(new TriviaCreadaEvento(trivia.Id, trivia.Nombre));
        return trivia;
    }

    public Pregunta AgregarPregunta(
        string enunciado,
        Puntaje puntaje,
        Tiempo tiempoEstimado,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones)
    {
        if (Estado == EstadoTrivia.Activa)
            throw new ExcepcionDominio("No se pueden agregar preguntas a una trivia que está activa.");
        if (_preguntas.Count >= 20)
            throw new ExcepcionDominio("La trivia no puede tener más de 20 preguntas.");
        if (tiempoEstimado is null)
            throw new ExcepcionDominio("El tiempo estimado de la pregunta es obligatorio.");
        if (tiempoEstimado.Valor > TiempoLimitePorPregunta.Valor)
            throw new ExcepcionDominio(
                "El tiempo de la pregunta no puede superar el límite configurado para la trivia.");

        var pregunta = Pregunta.Crear(Id, enunciado, puntaje, tiempoEstimado, opciones);
        _preguntas.Add(pregunta);
        return pregunta;
    }

    public void ModificarPregunta(
        Guid preguntaId,
        string nuevoEnunciado,
        Tiempo nuevoTiempoEstimado,
        IEnumerable<(string Texto, bool EsCorrecta)> nuevasOpciones)
    {
        if (Estado == EstadoTrivia.Activa)
            throw new ExcepcionDominio("No se pueden modificar preguntas a una trivia que está activa.");

        var pregunta = _preguntas.FirstOrDefault(p => p.Id == preguntaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pregunta con ID '{preguntaId}'.");

        if (nuevoTiempoEstimado is null)
            throw new ExcepcionDominio("El tiempo estimado de la pregunta es obligatorio.");
        if (nuevoTiempoEstimado.Valor > TiempoLimitePorPregunta.Valor)
            throw new ExcepcionDominio(
                "El tiempo de la pregunta no puede superar el límite configurado para la trivia.");

        pregunta.Modificar(nuevoEnunciado, nuevoTiempoEstimado, nuevasOpciones);
    }

    public void EliminarPregunta(Guid preguntaId)
    {
        if (Estado == EstadoTrivia.Activa)
            throw new ExcepcionDominio("No se pueden eliminar preguntas a una trivia que está activa.");

        var pregunta = _preguntas.FirstOrDefault(p => p.Id == preguntaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pregunta con ID '{preguntaId}'.");

        _preguntas.Remove(pregunta);
    }

    public void Activar()
    {
        if (Estado == EstadoTrivia.Activa)
            throw new ExcepcionDominio("La trivia ya está activa.");
        if (_preguntas.Count == 0)
            throw new ExcepcionDominio("La trivia debe tener al menos una pregunta para poder activarse.");

        Estado = EstadoTrivia.Activa;
        _eventos.Add(new TriviaActivadaEvento(Id, Nombre, _preguntas.Count));
    }

    public void Desactivar()
    {
        if (Estado == EstadoTrivia.Inactiva)
            throw new ExcepcionDominio("La trivia ya está inactiva.");

        Estado = EstadoTrivia.Inactiva;
        _eventos.Add(new TriviaArchivadaEvento(Id));
    }

    public void ModificarDatos(string nuevoNombre, string nuevaDescripcion, Tiempo nuevoTiempo)
    {
        if (Estado == EstadoTrivia.Activa)
            throw new ExcepcionDominio("No se puede modificar una trivia que está activa.");
        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ExcepcionDominio("El nombre de la trivia es obligatorio.");
        if (string.IsNullOrWhiteSpace(nuevaDescripcion))
            throw new ExcepcionDominio("La descripción de la trivia es obligatoria.");
        if (nuevoTiempo is null)
            throw new ExcepcionDominio("El tiempo límite por pregunta es obligatorio.");
        if (_preguntas.Count > 0 && nuevoTiempo.Valor < _preguntas.Max(p => p.TiempoEstimado.Valor))
            throw new ExcepcionDominio(
                "No se puede establecer un tiempo límite menor al tiempo de las preguntas existentes.");

        Nombre = nuevoNombre.Trim();
        Descripcion = nuevaDescripcion.Trim();
        TiempoLimitePorPregunta = nuevoTiempo;
        _eventos.Add(new TriviaModificadaEvento(Id, Nombre, TiempoLimitePorPregunta.Valor));
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
            TiempoLimitePorPregunta = Tiempo.DesdePersistencia(tiempoLimitePorPregunta),
            Estado = estado,
            FechaCreacion = fechaCreacion
        };
        trivia._preguntas.AddRange(preguntas);
        return trivia;
    }

    string IComponenteJuego.ObtenerDescripcion() => $"Trivia: {Nombre} [{Estado}]";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Array.Empty<IComponenteJuego>();

}
