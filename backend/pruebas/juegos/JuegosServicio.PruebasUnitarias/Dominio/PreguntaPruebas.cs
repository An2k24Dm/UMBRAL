using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU16: pruebas del comportamiento de Trivia.AgregarPregunta y la entidad Pregunta.
public class PreguntaPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Trivia TriviaEnBorrador() => Trivia.Crear(
        "Trivia de Geografía", "Descripción", CreadorId, 30, FechaFija);

    private static IEnumerable<(string Texto, bool EsCorrecta)> OpcionesValidas() =>
    [
        ("París", true),
        ("Madrid", false),
        ("Roma", false)
    ];

    [Fact]
    public void AgregarPregunta_ConDatosValidos_RetornaPreguntaConIdNoVacio()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, 10, OpcionesValidas());

        pregunta.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_AsignaTriviaId()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, 10, OpcionesValidas());

        pregunta.TriviaId.Should().Be(trivia.Id);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_AumentaConteoDePreguntas()
    {
        var trivia = TriviaEnBorrador();

        trivia.AgregarPregunta("Pregunta 1", 10, 10, OpcionesValidas());
        trivia.AgregarPregunta("Pregunta 2", 5, 10, OpcionesValidas());

        trivia.Preguntas.Should().HaveCount(2);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_AsignaOpciones()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, 10, OpcionesValidas());

        pregunta.Opciones.Should().HaveCount(3);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_MarcaUnicaOpcionCorrectaCorrectamente()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, 10, OpcionesValidas());

        pregunta.Opciones.Should().ContainSingle(o => o.EsCorrecta);
        pregunta.Opciones.First(o => o.EsCorrecta).Texto.Should().Be("París");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPregunta_EnunciadoVacio_LanzaExcepcionDominio(string enunciado)
    {
        var trivia = TriviaEnBorrador();

        Action accion = () => trivia.AgregarPregunta(enunciado, 10, 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }

    // Regla: puntaje múltiplo de 5, máximo 100
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(11)]
    public void AgregarPregunta_PuntajeNoMultiploDe5_LanzaExcepcionDominio(int puntaje)
    {
        var trivia = TriviaEnBorrador();

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", puntaje, 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_PuntajeMayorA100_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", 105, 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(100)]
    public void AgregarPregunta_PuntajeValidoMultiploDe5_NoLanzaExcepcion(int puntaje)
    {
        var trivia = TriviaEnBorrador();

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", puntaje, 10, OpcionesValidas());

        accion.Should().NotThrow();
    }

    [Fact]
    public void AgregarPregunta_MenosDeDosopciones_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        var soloUna = new[] { ("París", true) };

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", 10, 10, soloUna);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_SinOpcionCorrecta_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        var sinCorrecta = new[] { ("París", false), ("Madrid", false) };

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", 10, 10, sinCorrecta);

        accion.Should().Throw<ExcepcionDominio>();
    }

    // Regla: una sola respuesta correcta
    [Fact]
    public void AgregarPregunta_MasDeUnaOpcionCorrecta_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        var dosCorrectas = new[] { ("París", true), ("Madrid", true), ("Roma", false) };

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", 10, 10, dosCorrectas);

        accion.Should().Throw<ExcepcionDominio>();
    }

    // Regla: máximo 20 preguntas
    [Fact]
    public void AgregarPregunta_Mas20Preguntas_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        for (var i = 1; i <= 20; i++)
            trivia.AgregarPregunta($"Pregunta {i}", 5, 10, OpcionesValidas());

        Action accion = () => trivia.AgregarPregunta("Pregunta 21", 5, 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_Exactamente20Preguntas_NoLanzaExcepcion()
    {
        var trivia = TriviaEnBorrador();
        for (var i = 1; i <= 19; i++)
            trivia.AgregarPregunta($"Pregunta {i}", 5, 10, OpcionesValidas());

        Action accion = () => trivia.AgregarPregunta("Pregunta 20", 5, 10, OpcionesValidas());

        accion.Should().NotThrow();
    }

    [Fact]
    public void AgregarPregunta_TriviaNoEnBorrador_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        trivia.AgregarPregunta("Pregunta inicial", 10, 10, OpcionesValidas());
        trivia.Activar();

        Action accion = () => trivia.AgregarPregunta("Nueva pregunta", 10, 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }
}
