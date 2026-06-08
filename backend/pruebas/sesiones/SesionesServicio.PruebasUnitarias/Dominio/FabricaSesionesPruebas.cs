using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Factorias;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class FabricaSesionesPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void CrearIndividual_DevuelveSesionIndividual()
    {
        var sesion = FabricaSesiones.CrearIndividual(
            "Piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc);

        sesion.Should().BeOfType<SesionIndividual>();
        sesion.TipoSesion.Should().Be("Individual");
    }

    [Fact]
    public void CrearGrupal_DevuelveSesionGrupal()
    {
        var sesion = FabricaSesiones.CrearGrupal(
            "Piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc);

        sesion.Should().BeOfType<SesionGrupal>();
        sesion.TipoSesion.Should().Be("Grupal");
    }

    [Fact]
    public void Crear_DejaSesionEnEstadoProgramada()
    {
        var ind = FabricaSesiones.CrearIndividual("A", "Demo", AhoraUtc.AddHours(1), "ABC", Operador, AhoraUtc);
        var grp = FabricaSesiones.CrearGrupal("B", "Demo", AhoraUtc.AddHours(1), "DEF", Operador, AhoraUtc);

        ind.Estado.Should().Be(EstadoSesion.Programada);
        grp.Estado.Should().Be(EstadoSesion.Programada);
    }

    [Fact]
    public void Crear_NaceSinParticipantesNiEquipos()
    {
        var ind = FabricaSesiones.CrearIndividual("A", "Demo", AhoraUtc.AddHours(1), "ABC", Operador, AhoraUtc);
        var grp = FabricaSesiones.CrearGrupal("B", "Demo", AhoraUtc.AddHours(1), "DEF", Operador, AhoraUtc);

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
        Action accion = () => FabricaSesiones.CrearIndividual(
            nombre, "Demo", AhoraUtc.AddHours(1), "ABC", Operador, AhoraUtc);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void Crear_DescripcionVacia_Lanza()
    {
        Action accion = () => FabricaSesiones.CrearGrupal(
            "A", "", AhoraUtc.AddHours(1), "ABC", Operador, AhoraUtc);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void Crear_CodigoVacio_Lanza()
    {
        Action accion = () => FabricaSesiones.CrearIndividual(
            "A", "Demo", AhoraUtc.AddHours(1), "", Operador, AhoraUtc);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void Crear_OperadorVacio_Lanza()
    {
        Action accion = () => FabricaSesiones.CrearIndividual(
            "A", "Demo", AhoraUtc.AddHours(1), "ABC", Guid.Empty, AhoraUtc);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }
}
