using JuegosServicio.Aplicacion.Comandos.AgregarPregunta;
using JuegosServicio.Aplicacion.Comandos.CrearBusquedaTesoro;
using JuegosServicio.Aplicacion.Comandos.CrearTrivia;
using JuegosServicio.Aplicacion.Comandos.ModificarBusquedaTesoro;
using JuegosServicio.Aplicacion.Comandos.ModificarTrivia;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasUnitarias.Validaciones;

// Cobertura de las reglas de tiempo en la capa de validación de aplicación.
public class ValidadoresTiempoPruebas
{
    private static List<OpcionDto> OpcionesValidas() =>
    [
        new() { Texto = "A", EsCorrecta = true },
        new() { Texto = "B", EsCorrecta = false }
    ];

    [Fact]
    public void ValidadorCrearTrivia_TiempoMayorA60_AgregaError()
    {
        var comando = new CrearTriviaComando(new CrearTriviaDto
        {
            Nombre = "Trivia",
            Descripcion = "Descripción",
            TiempoLimitePorPregunta = 61
        }, Guid.NewGuid());

        var resultado = new ValidadorCrearTrivia().Validar(comando);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Campo == "tiempoLimitePorPregunta");
    }

    [Fact]
    public void ValidadorModificarTrivia_TiempoMayorA60_AgregaError()
    {
        var comando = new ModificarTriviaComando(Guid.NewGuid(), new ModificarTriviaDto
        {
            NuevoNombre = "Trivia",
            NuevaDescripcion = "Descripción",
            NuevoTiempoLimitePorPregunta = 61
        });

        var resultado = new ValidadorModificarTrivia().Validar(comando);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Campo == "nuevoTiempoLimitePorPregunta");
    }

    [Fact]
    public void ValidadorAgregarPregunta_TiempoMayorA60_AgregaError()
    {
        var comando = new AgregarPreguntaComando(Guid.NewGuid(), new AgregarPreguntaDto
        {
            Enunciado = "¿?",
            PuntajeAsignado = 10,
            TiempoEstimado = 61,
            Opciones = OpcionesValidas()
        });

        var resultado = new ValidadorAgregarPregunta().Validar(comando);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Campo == "tiempoEstimado");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(61)]
    public void ValidadorCrearBusquedaTesoro_TiempoFueraDeRango_AgregaError(int tiempo)
    {
        var comando = new CrearBusquedaTesoroComando(new CrearBusquedaTesoroDto
        {
            Nombre = "Búsqueda",
            Descripcion = "Descripción",
            Tiempo = tiempo,
            Puntaje = 50
        }, Guid.NewGuid());

        var resultado = new ValidadorCrearBusquedaTesoro().Validar(comando);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Campo == "tiempo");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    public void ValidadorCrearBusquedaTesoro_TiempoEnRango_SinErrorDeTiempo(int tiempo)
    {
        var comando = new CrearBusquedaTesoroComando(new CrearBusquedaTesoroDto
        {
            Nombre = "Búsqueda",
            Descripcion = "Descripción",
            Tiempo = tiempo,
            Puntaje = 50
        }, Guid.NewGuid());

        var resultado = new ValidadorCrearBusquedaTesoro().Validar(comando);

        resultado.Errores.Should().NotContain(e => e.Campo == "tiempo");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(61)]
    public void ValidadorModificarBusquedaTesoro_TiempoFueraDeRango_AgregaError(int tiempo)
    {
        var comando = new ModificarBusquedaTesoroComando(Guid.NewGuid(), new ModificarBusquedaTesoroDto
        {
            Nombre = "Búsqueda",
            Descripcion = "Descripción",
            Tiempo = tiempo,
            Puntaje = 50
        });

        var resultado = new ValidadorModificarBusquedaTesoro().Validar(comando);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Campo == "tiempo");
    }
}
