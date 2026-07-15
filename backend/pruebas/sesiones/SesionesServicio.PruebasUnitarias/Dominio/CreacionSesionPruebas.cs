using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Construcción de sesiones a nivel de dominio (SesionIndividual.Crear /
// SesionGrupal.Crear), incluida la capacidad configurable. La selección por
// modo en producción la resuelve la fábrica IFabricaSesion/FabricaSesion,
// cubierta en CrearSesionManejadorPruebas.
public class CreacionSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionIndividual Individual(
        string nombre = "Piloto", string descripcion = "Demo",
        string codigo = "ABC123", Guid? operador = null,
        int maximoParticipantes = 10)
        => SesionIndividual.Crear(
            nombre, descripcion, AhoraUtc.AddHours(1), codigo,
            operador ?? Operador, AhoraUtc, maximoParticipantes);

    private static SesionGrupal Grupal(
        string nombre = "Piloto", string descripcion = "Demo",
        string codigo = "DEF456", Guid? operador = null,
        int maximoEquipos = 5, int maximoParticipantesPorEquipo = 2)
        => SesionGrupal.Crear(
            nombre, descripcion, AhoraUtc.AddHours(1), codigo,
            operador ?? Operador, AhoraUtc, maximoEquipos, maximoParticipantesPorEquipo);

    [Fact]
    public void CrearIndividual_DevuelveSesionIndividual()
    {
        var sesion = Individual();

        sesion.Should().BeOfType<SesionIndividual>();
        sesion.TipoSesion.Should().Be("Individual");
        sesion.MaximoParticipantes.Should().Be(10);
    }

    [Fact]
    public void CrearGrupal_DevuelveSesionGrupal()
    {
        var sesion = Grupal();

        sesion.Should().BeOfType<SesionGrupal>();
        sesion.TipoSesion.Should().Be("Grupal");
        sesion.MaximoEquipos.Should().Be(5);
        sesion.MaximoParticipantesPorEquipo.Should().Be(2);
    }

    [Fact]
    public void Crear_DejaSesionEnEstadoProgramada()
    {
        Individual().Estado.Should().Be(EstadoSesion.Programada);
        Grupal().Estado.Should().Be(EstadoSesion.Programada);
    }

    [Fact]
    public void Crear_NaceSinParticipantesNiEquipos()
    {
        var ind = Individual();
        var grp = Grupal();

        ind.Participantes.Should().BeEmpty();
        grp.Equipos.Should().BeEmpty();
        ind.Misiones.Should().BeEmpty();
        grp.Misiones.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_NombreVacio_Lanza(string nombre)
    {
        Action accion = () => Individual(nombre: nombre);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void Crear_DescripcionVacia_Lanza()
    {
        Action accion = () => Grupal(descripcion: "");
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void Crear_CodigoVacio_Lanza()
    {
        Action accion = () => Individual(codigo: "");
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void Crear_OperadorVacio_Lanza()
    {
        Action accion = () => Individual(operador: Guid.Empty);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CrearIndividual_CapacidadNoPositiva_Lanza(int maximo)
    {
        Action accion = () => Individual(maximoParticipantes: maximo);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(5, 0)]
    [InlineData(-1, 2)]
    public void CrearGrupal_CapacidadNoPositiva_Lanza(int equipos, int porEquipo)
    {
        Action accion = () => Grupal(
            maximoEquipos: equipos, maximoParticipantesPorEquipo: porEquipo);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }
}
