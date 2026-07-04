using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Invariantes de dominio de Trivia relacionadas al tiempo: la pregunta no
// puede superar el límite configurado en su propia trivia y no se puede
// bajar el límite por debajo de las preguntas existentes. El tope absoluto
// de 60 s vive en la capa de aplicación, no aquí.
public class TriviaTiempoPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static IEnumerable<(string Texto, bool EsCorrecta)> Opciones() =>
    [
        ("A", true),
        ("B", false)
    ];

    private static Trivia TriviaConLimite(int limite) =>
        Trivia.Crear("Trivia", "Descripción", CreadorId, Tiempo.CrearPositivo(limite), FechaFija);

    [Fact]
    public void AgregarPregunta_TiempoMayorAlLimiteDeLaTrivia_Lanza()
    {
        var trivia = TriviaConLimite(40);
        Action accion = () => trivia.AgregarPregunta(
            "¿?", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(50), Opciones());
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPregunta_LimiteCuarenta_PreguntaCuarenta_NoLanza()
    {
        var trivia = TriviaConLimite(40);
        Action accion = () => trivia.AgregarPregunta(
            "¿?", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(40), Opciones());
        accion.Should().NotThrow();
    }

    [Fact]
    public void ModificarPregunta_TiempoMayorAlLimiteDeLaTrivia_Lanza()
    {
        var trivia = TriviaConLimite(40);
        var pregunta = trivia.AgregarPregunta(
            "¿?", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(30), Opciones());

        Action accion = () => trivia.ModificarPregunta(
            pregunta.Id, "¿?", Tiempo.CrearParaPregunta(50), Opciones());
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarDatos_BajarLimitePorDebajoDePreguntasExistentes_Lanza()
    {
        var trivia = TriviaConLimite(50);
        trivia.AgregarPregunta(
            "¿?", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(45), Opciones());

        Action accion = () => trivia.ModificarDatos(
            "Trivia", "Descripción", Tiempo.CrearPositivo(40));
        accion.Should().Throw<ExcepcionDominio>();
    }
}
