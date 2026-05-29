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

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, OpcionesValidas());

        pregunta.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_AsignaTriviaId()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, OpcionesValidas());

        pregunta.TriviaId.Should().Be(trivia.Id);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_AumentaConteoDePreguntas()
    {
        var trivia = TriviaEnBorrador();

        trivia.AgregarPregunta("Pregunta 1", 10, OpcionesValidas());
        trivia.AgregarPregunta("Pregunta 2", 5, OpcionesValidas());

        trivia.Preguntas.Should().HaveCount(2);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_AsignaOpciones()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, OpcionesValidas());

        pregunta.Opciones.Should().HaveCount(3);
    }

    [Fact]
    public void AgregarPregunta_ConDatosValidos_MarcaOpcionCorrectaCorrectamente()
    {
        var trivia = TriviaEnBorrador();

        var pregunta = trivia.AgregarPregunta("¿Capital de Francia?", 10, OpcionesValidas());

        pregunta.Opciones.Should().ContainSingle(o => o.EsCorrecta);
        pregunta.Opciones.First(o => o.EsCorrecta).Texto.Should().Be("París");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPregunta_EnunciadoVacio_LanzaExcepcionDominio(string enunciado)
    {
        var trivia = TriviaEnBorrador();

        Action accion = () => trivia.AgregarPregunta(enunciado, 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AgregarPregunta_PuntajeMenorOIgualACero_LanzaExcepcionDominio(int puntaje)
    {
        var trivia = TriviaEnBorrador();

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", puntaje, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_MenosDeDosopciones_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        var soloUna = new[] { ("París", true) };

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", 10, soloUna);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_SinOpcionCorrecta_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        var sinCorrecta = new[] { ("París", false), ("Madrid", false) };

        Action accion = () => trivia.AgregarPregunta("¿Pregunta?", 10, sinCorrecta);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_TriviaNoEnBorrador_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        trivia.AgregarPregunta("Pregunta inicial", 10, OpcionesValidas());
        trivia.Activar();

        Action accion = () => trivia.AgregarPregunta("Nueva pregunta", 10, OpcionesValidas());

        accion.Should().Throw<ExcepcionDominio>();
    }
}
