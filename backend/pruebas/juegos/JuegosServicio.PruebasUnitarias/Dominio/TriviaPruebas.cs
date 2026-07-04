using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU15: pruebas del aggregate root Trivia — creación y estado inicial.
public class TriviaPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Trivia TriviaValida() => Trivia.Crear(
        "Trivia de Geografía",
        "Preguntas sobre capitales del mundo",
        CreadorId,
        tiempoLimitePorPregunta: Tiempo.CrearPositivo(30),
        FechaFija);

    [Fact]
    public void Crear_ConDatosValidos_RetornaEstadoBorrador()
    {
        var trivia = TriviaValida();
        trivia.Estado.Should().Be(EstadoTrivia.Inactiva);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaIdNoVacio()
    {
        var trivia = TriviaValida();
        trivia.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaCreadorId()
    {
        var trivia = TriviaValida();
        trivia.CreadorId.Should().Be(CreadorId);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaTiempoLimite()
    {
        var trivia = TriviaValida();
        trivia.TiempoLimitePorPregunta.Valor.Should().Be(30);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaFechaCreacion()
    {
        var trivia = TriviaValida();
        trivia.FechaCreacion.Should().Be(FechaFija);
    }

    [Fact]
    public void Crear_ConEspaciosEnNombre_NormalizaConTrim()
    {
        var trivia = Trivia.Crear(
            "  Trivia de Geografía  ",
            "Descripción válida",
            CreadorId, Tiempo.CrearPositivo(30), FechaFija);

        trivia.Nombre.Should().Be("Trivia de Geografía");
    }

    [Fact]
    public void Crear_ConEspaciosEnDescripcion_NormalizaConTrim()
    {
        var trivia = Trivia.Crear(
            "Trivia válida",
            "  Descripción con espacios  ",
            CreadorId, Tiempo.CrearPositivo(30), FechaFija);

        trivia.Descripcion.Should().Be("Descripción con espacios");
    }

    [Fact]
    public void Crear_ConDatosValidos_ListaDePreguntasVacia()
    {
        var trivia = TriviaValida();
        trivia.Preguntas.Should().BeEmpty();
    }

    [Fact]
    public void Crear_ConDatosValidos_GeneraUnEventoTriviaCreadaEnMemoria()
    {
        var trivia = TriviaValida();
        trivia.Eventos.Should().HaveCount(1);
        trivia.Eventos[0].Should().BeOfType<TriviaCreadaEvento>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_NombreVacioOEspacios_LanzaExcepcionDominio(string nombre)
    {
        Action accion = () => Trivia.Crear(
            nombre, "Descripción", CreadorId, Tiempo.CrearPositivo(30), FechaFija);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_DescripcionVaciaOEspacios_LanzaExcepcionDominio(string descripcion)
    {
        Action accion = () => Trivia.Crear(
            "Nombre", descripcion, CreadorId, Tiempo.CrearPositivo(30), FechaFija);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Crear_CreadorIdVacio_LanzaExcepcionDominio()
    {
        Action accion = () => Trivia.Crear(
            "Nombre", "Descripción", Guid.Empty, Tiempo.CrearPositivo(30), FechaFija);
        accion.Should().Throw<ExcepcionDominio>();
    }

    // La regla "mayor a cero" ahora vive en el objeto de valor Tiempo.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Crear_TiempoLimiteMenorOIgualACero_LanzaExcepcionDominio(int tiempo)
    {
        Action accion = () => Trivia.Crear(
            "Nombre", "Descripción", CreadorId, Tiempo.CrearPositivo(tiempo), FechaFija);
        accion.Should().Throw<ExcepcionDominio>();
    }
}
