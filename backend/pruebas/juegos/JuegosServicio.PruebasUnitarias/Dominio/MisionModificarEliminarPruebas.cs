using JuegosServicio.Dominio.Dificultades;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Pruebas del aggregate Mision — AgregarEtapa y EliminarEtapa.
public class MisionModificarEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Mision MisionInactiva() =>
        Mision.Crear("Misión Test", "Descripción", Guid.NewGuid(), FechaFija);

    // --- AgregarEtapa ---

    [Fact]
    public void AgregarEtapa_EnEstadoInactiva_AgregaLaEtapa()
    {
        var mision = MisionInactiva();

        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());

        mision.Etapas.Should().HaveCount(1);
    }

    [Fact]
    public void AgregarEtapa_MultipleEtapas_OrdenCorrelativo()
    {
        var mision = MisionInactiva();

        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        mision.AgregarEtapa(TipoModoDeJuego.BusquedaTesoro, Guid.NewGuid());

        mision.Etapas[0].Orden.Should().Be(1);
        mision.Etapas[1].Orden.Should().Be(2);
    }

    [Fact]
    public void AgregarEtapa_EnEstadoActiva_LanzaExcepcionDominio()
    {
        var mision = MisionInactiva();
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        mision.Activar();

        Action accion = () => mision.AgregarEtapa(TipoModoDeJuego.BusquedaTesoro, Guid.NewGuid());

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarEtapa_ModoDeJuegoIdVacio_LanzaExcepcionDominio()
    {
        var mision = MisionInactiva();

        Action accion = () => mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.Empty);

        accion.Should().Throw<ExcepcionDominio>();
    }

    // --- EliminarEtapa ---

    [Fact]
    public void EliminarEtapa_EnEstadoInactiva_QuitaLaEtapa()
    {
        var mision = MisionInactiva();
        var etapa = mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());

        mision.EliminarEtapa(etapa.Id);

        mision.Etapas.Should().BeEmpty();
    }

    [Fact]
    public void EliminarEtapa_RenumeraOrdenes()
    {
        var mision = MisionInactiva();
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        var etapa2 = mision.AgregarEtapa(TipoModoDeJuego.BusquedaTesoro, Guid.NewGuid());
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());

        mision.EliminarEtapa(etapa2.Id);

        mision.Etapas.Should().HaveCount(2);
        mision.Etapas[0].Orden.Should().Be(1);
        mision.Etapas[1].Orden.Should().Be(2);
    }

    [Fact]
    public void EliminarEtapa_EnEstadoActiva_LanzaExcepcionDominio()
    {
        var mision = MisionInactiva();
        var etapa = mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        mision.Activar();

        Action accion = () => mision.EliminarEtapa(etapa.Id);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void EliminarEtapa_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var mision = MisionInactiva();

        Action accion = () => mision.EliminarEtapa(Guid.NewGuid());

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    // --- Modificar ---

    [Fact]
    public void Modificar_EnEstadoInactiva_ActualizaNombreYDescripcion()
    {
        var mision = MisionInactiva();

        mision.Modificar("Nuevo nombre", "Nueva descripción", NivelDificultad.Baja);

        mision.Nombre.Should().Be("Nuevo nombre");
        mision.Descripcion.Should().Be("Nueva descripción");
    }

    [Fact]
    public void Modificar_EnEstadoInactiva_ActualizaDificultad()
    {
        var mision = MisionInactiva();

        mision.Modificar("Nombre", "Descripción", NivelDificultad.Dificil);

        mision.Dificultad.Should().Be(NivelDificultad.Dificil);
        mision.ObtenerDificultad().Should().BeOfType<DificultadDificil>();
    }

    [Fact]
    public void Modificar_EnEstadoActiva_LanzaExcepcionDominio()
    {
        var mision = MisionInactiva();
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        mision.Activar();

        Action accion = () => mision.Modificar("Nombre", "Descripción", NivelDificultad.Media);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Modificar_NombreVacio_LanzaExcepcionDominio(string nombre)
    {
        var mision = MisionInactiva();

        Action accion = () => mision.Modificar(nombre, "Descripción", NivelDificultad.Media);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
