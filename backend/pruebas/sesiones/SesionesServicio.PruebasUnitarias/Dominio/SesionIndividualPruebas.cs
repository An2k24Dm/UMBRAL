using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class SesionIndividualPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const int MaximoParticipantes = 10;

    private static SesionIndividual Crear(int maximoParticipantes = MaximoParticipantes)
        => SesionIndividual.Crear(
            "Sesión piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoParticipantes);

    [Fact]
    public void Crear_GuardaLaCapacidadConfigurada()
    {
        var sesion = Crear(7);
        sesion.MaximoParticipantes.Should().Be(7);
    }

    [Fact]
    public void AgregarParticipante_LoIncluyeConDatosCorrectos()
    {
        var sesion = Crear();
        sesion.Preparar();
        var pid = Guid.NewGuid();

        var p = sesion.AgregarParticipante(pid, AhoraUtc);

        p.ParticipanteIdentidadId.Should().Be(pid);
        p.EquipoId.Should().BeNull();
        p.FechaUnionSesion.Should().Be(AhoraUtc);
        p.FechaUnionEquipo.Should().BeNull();
        p.Puntaje.Should().Be(0);
        sesion.Participantes.Should().HaveCount(1);
    }

    [Fact]
    public void AgregarParticipante_IdVacio_Lanza()
    {
        var sesion = Crear();
        sesion.Preparar();
        Action accion = () => sesion.AgregarParticipante(Guid.Empty, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AgregarParticipante_Duplicado_Lanza()
    {
        var sesion = Crear();
        sesion.Preparar();
        var pid = Guid.NewGuid();
        sesion.AgregarParticipante(pid, AhoraUtc);

        Action accion = () => sesion.AgregarParticipante(pid, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AgregarParticipante_MasDelMaximo_Lanza()
    {
        // La capacidad ahora es propia de la sesión, no una constante global.
        var sesion = Crear(maximoParticipantes: 3);
        sesion.Preparar();
        for (var i = 0; i < 3; i++)
            sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);

        Action accion = () => sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>()
            .WithMessage("La sesión individual alcanzó el máximo de participantes permitido.");
    }

    [Fact]
    public void AgregarParticipante_SesionNoEnPreparacion_Lanza()
    {
        var sesion = Crear();

        Action accion = () => sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);

        accion.Should().Throw<ParticipacionInvalidaExcepcion>()
            .WithMessage("Solo puedes ingresar a una sesión en estado En Preparación.");
    }

    [Fact]
    public void AsignarMisiones_AceptaEntre1Y5()
    {
        var sesion = Crear();
        var lista = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        sesion.AsignarMisiones(lista);
        sesion.Misiones.Should().HaveCount(5);
    }
}
