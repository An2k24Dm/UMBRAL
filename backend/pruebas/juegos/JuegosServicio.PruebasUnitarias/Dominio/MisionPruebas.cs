using JuegosServicio.Dominio.Dificultades;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Pruebas del aggregate Mision — creación y estado inicial.
public class MisionPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Mision MisionValida() =>
        Mision.Crear("Misión del Parque", "Recorre el parque completando etapas", CreadorId, FechaFija);

    [Fact]
    public void Crear_ConDatosValidos_RetornaEstadoInactiva()
    {
        var mision = MisionValida();
        mision.Estado.Should().Be(EstadoMision.Inactiva);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaIdNoVacio()
    {
        var mision = MisionValida();
        mision.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Crear_ConDatosValidos_EtapasVacias()
    {
        var mision = MisionValida();
        mision.Etapas.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_NombreVacioOEspacios_LanzaExcepcionDominio(string nombre)
    {
        Action accion = () => Mision.Crear(nombre, "Desc", CreadorId, FechaFija);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Crear_CreadorIdVacio_LanzaExcepcionDominio()
    {
        Action accion = () => Mision.Crear("Nombre", "Desc", Guid.Empty, FechaFija);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_ConEtapas_CambiaEstadoAActiva()
    {
        var mision = MisionValida();
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());

        mision.Activar();

        mision.Estado.Should().Be(EstadoMision.Activa);
    }

    [Fact]
    public void Activar_SinEtapas_LanzaExcepcionDominio()
    {
        var mision = MisionValida();

        Action accion = () => mision.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Desactivar_MisionActiva_CambiaEstadoAInactiva()
    {
        var mision = MisionValida();
        mision.AgregarEtapa(TipoModoDeJuego.BusquedaTesoro, Guid.NewGuid());
        mision.Activar();

        mision.Desactivar();

        mision.Estado.Should().Be(EstadoMision.Inactiva);
    }

    [Fact]
    public void Desactivar_MisionInactiva_LanzaExcepcionDominio()
    {
        var mision = MisionValida();

        Action accion = () => mision.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    // --- Dificultad ---

    [Fact]
    public void Crear_SinEspecificarDificultad_DefaultEsMedia()
    {
        var mision = MisionValida();
        mision.Dificultad.Should().Be(NivelDificultad.Media);
    }

    [Theory]
    [InlineData(NivelDificultad.Baja)]
    [InlineData(NivelDificultad.Media)]
    [InlineData(NivelDificultad.Dificil)]
    public void Crear_ConDificultadEspecificada_AsignaElNivelCorrecto(NivelDificultad nivel)
    {
        var mision = Mision.Crear("Misión", "Desc", CreadorId, FechaFija, nivel);
        mision.Dificultad.Should().Be(nivel);
    }

    [Fact]
    public void ObtenerDificultad_Baja_RetornaDificultadBaja()
    {
        var mision = Mision.Crear("Misión", "Desc", CreadorId, FechaFija, NivelDificultad.Baja);
        mision.ObtenerDificultad().Should().BeOfType<DificultadBaja>();
        mision.ObtenerDificultad().Nombre.Should().Be("Baja");
    }

    [Fact]
    public void ObtenerDificultad_Media_RetornaDificultadMedia()
    {
        var mision = Mision.Crear("Misión", "Desc", CreadorId, FechaFija, NivelDificultad.Media);
        mision.ObtenerDificultad().Should().BeOfType<DificultadMedia>();
        mision.ObtenerDificultad().Nombre.Should().Be("Media");
    }

    [Fact]
    public void ObtenerDificultad_Dificil_RetornaDificultadDificil()
    {
        var mision = Mision.Crear("Misión", "Desc", CreadorId, FechaFija, NivelDificultad.Dificil);
        mision.ObtenerDificultad().Should().BeOfType<DificultadDificil>();
        mision.ObtenerDificultad().Nombre.Should().Be("Difícil");
    }

    [Fact]
    public void FabricaDificultadMision_NivelInvalido_LanzaArgumentOutOfRange()
    {
        Action accion = () => FabricaDificultadMision.Obtener((NivelDificultad)99);
        accion.Should().Throw<ArgumentOutOfRangeException>();
    }
}
