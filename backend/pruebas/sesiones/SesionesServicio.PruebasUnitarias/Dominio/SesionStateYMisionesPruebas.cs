using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Cubre el patrón State y la asignación de misiones a nivel del padre
// abstracto Sesion. Ambas hijas deben heredar el mismo comportamiento.
public class SesionStateYMisionesPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionIndividual NuevaIndividual()
        => SesionIndividual.Crear(
            "I", "Demo", AhoraUtc.AddHours(1), "I-ABC", Operador, AhoraUtc,
            maximoParticipantes: 10);

    private static SesionGrupal NuevaGrupal()
        => SesionGrupal.Crear(
            "G", "Demo", AhoraUtc.AddHours(1), "G-DEF", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);

    [Fact]
    public void Individual_TransitaProgramadaAEnPreparacionActivaPausadaActivaFinalizada()
    {
        var s = NuevaIndividual();
        s.Estado.Should().Be(EstadoSesion.Programada);
        s.Preparar();
        s.Estado.Should().Be(EstadoSesion.EnPreparacion);
        s.Iniciar(AhoraUtc);
        s.Estado.Should().Be(EstadoSesion.Activa);
        s.FechaInicioUtc.Should().Be(AhoraUtc);
        s.Pausar();
        s.Estado.Should().Be(EstadoSesion.Pausada);
        s.Reanudar();
        s.Estado.Should().Be(EstadoSesion.Activa);
        s.Finalizar(AhoraUtc.AddMinutes(30));
        s.Estado.Should().Be(EstadoSesion.Finalizada);
        s.FechaFinalizacionUtc.Should().Be(AhoraUtc.AddMinutes(30));
    }

    [Fact]
    public void Grupal_TransitaIgualQueIndividual()
    {
        var s = NuevaGrupal();
        s.Preparar();
        s.Iniciar(AhoraUtc);
        s.Pausar();
        s.Reanudar();
        s.Finalizar(AhoraUtc.AddMinutes(30));
        s.Estado.Should().Be(EstadoSesion.Finalizada);
    }

    [Fact]
    public void TransicionInvalida_DesdeProgramada_Iniciar_Lanza()
    {
        var s = NuevaIndividual();
        Action accion = () => s.Iniciar(AhoraUtc);
        accion.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    [Fact]
    public void Cancelar_DesdeProgramada_Funciona()
    {
        var s = NuevaGrupal();
        s.Cancelar();
        s.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void AsignarMisiones_SinMisiones_Lanza()
    {
        var s = NuevaIndividual();
        Action accion = () => s.AsignarMisiones(Array.Empty<Guid>());
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_MasDelMaximo_Lanza()
    {
        var s = NuevaGrupal();
        var lista = Enumerable.Range(0,
            PoliticaCapacidadSesion.MaximoMisionesPorSesion + 1)
            .Select(_ => Guid.NewGuid()).ToList();
        Action accion = () => s.AsignarMisiones(lista);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_Repetidas_Lanza()
    {
        var s = NuevaIndividual();
        var repetida = Guid.NewGuid();
        Action accion = () => s.AsignarMisiones(new[] { repetida, repetida });
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_GuidEmpty_Lanza()
    {
        var s = NuevaIndividual();
        Action accion = () => s.AsignarMisiones(new[] { Guid.Empty });
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }
}
