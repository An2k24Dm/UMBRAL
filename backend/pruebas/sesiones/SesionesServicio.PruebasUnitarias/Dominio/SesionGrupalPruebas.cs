using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class SesionGrupalPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const int MaximoEquipos = 5;
    private const int MaximoParticipantesPorEquipo = 2;

    private static SesionGrupal Crear(
        int maximoEquipos = MaximoEquipos,
        int maximoParticipantesPorEquipo = MaximoParticipantesPorEquipo)
        => SesionGrupal.Crear(
            "Sesión piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos, maximoParticipantesPorEquipo);

    [Fact]
    public void Crear_GuardaLasCapacidadesConfiguradas()
    {
        var sesion = Crear(maximoEquipos: 4, maximoParticipantesPorEquipo: 3);
        sesion.MaximoEquipos.Should().Be(4);
        sesion.MaximoParticipantesPorEquipo.Should().Be(3);
    }

    [Fact]
    public void CrearEquipo_CreaLiderYLoIncluye()
    {
        var sesion = Crear();
        var lider = Guid.NewGuid();

        var equipo = sesion.CrearEquipo("Rojo", lider, AhoraUtc, AhoraUtc);

        equipo.Nombre.Valor.Should().Be("Rojo");
        equipo.Puntaje.Should().Be(0);
        equipo.LiderParticipanteId.Should().Be(equipo.Participantes.Single().Id);
        equipo.Participantes.Single().ParticipanteIdentidadId.Should().Be(lider);
        equipo.Participantes.Single().EquipoId.Should().Be(equipo.Id);
        sesion.Equipos.Should().ContainSingle(e => e.Id == equipo.Id);
    }

    [Fact]
    public void CrearEquipo_NombreVacio_Lanza()
    {
        var sesion = Crear();
        Action accion = () => sesion.CrearEquipo("   ", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void CrearEquipo_NombreRepetido_Lanza()
    {
        var sesion = Crear();
        sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);

        Action accion = () => sesion.CrearEquipo("rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void CrearEquipo_SuperaMaximo_Lanza()
    {
        // El tope de equipos lo define la propia sesión.
        var sesion = Crear(maximoEquipos: 3);
        for (var i = 0; i < 3; i++)
            sesion.CrearEquipo($"Equipo{i}", Guid.NewGuid(), AhoraUtc, AhoraUtc);

        Action accion = () => sesion.CrearEquipo("Extra", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        accion.Should().Throw<EquipoInvalidoExcepcion>()
            .WithMessage("La sesión grupal alcanzó el máximo de equipos permitido.");
    }

    [Fact]
    public void CrearEquipo_LiderYaEnOtroEquipo_Lanza()
    {
        var sesion = Crear();
        var lider = Guid.NewGuid();
        sesion.CrearEquipo("Rojo", lider, AhoraUtc, AhoraUtc);

        Action accion = () => sesion.CrearEquipo("Azul", lider, AhoraUtc, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AgregarParticipanteAEquipo_LlenaConDos()
    {
        var sesion = Crear();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);

        var nuevo = Guid.NewGuid();
        var participante = sesion.AgregarParticipanteAEquipo(equipo.Id, nuevo, AhoraUtc, AhoraUtc);

        equipo.Participantes.Should().HaveCount(2);
        participante.EquipoId.Should().Be(equipo.Id);
        participante.FechaUnionEquipo.Should().NotBeNull();
    }

    [Fact]
    public void AgregarParticipanteAEquipo_SuperaDos_Lanza()
    {
        var sesion = Crear();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);

        Action accion = () => sesion.AgregarParticipanteAEquipo(
            equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void AgregarParticipanteAEquipo_YaEnOtroEquipo_Lanza()
    {
        var sesion = Crear();
        var participante = Guid.NewGuid();
        sesion.CrearEquipo("Rojo", participante, AhoraUtc, AhoraUtc);
        var equipoAzul = sesion.CrearEquipo("Azul", Guid.NewGuid(), AhoraUtc, AhoraUtc);

        Action accion = () => sesion.AgregarParticipanteAEquipo(
            equipoAzul.Id, participante, AhoraUtc, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AgregarParticipanteAEquipo_EquipoInexistente_Lanza()
    {
        var sesion = Crear();
        Action accion = () => sesion.AgregarParticipanteAEquipo(
            Guid.NewGuid(), Guid.NewGuid(), AhoraUtc, AhoraUtc);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    // HU47 — El identificador del participante es obligatorio.
    [Fact]
    public void AgregarParticipanteAEquipo_ParticipanteVacio_Lanza()
    {
        var sesion = Crear();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);

        Action accion = () => sesion.AgregarParticipanteAEquipo(
            equipo.Id, Guid.Empty, AhoraUtc, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }
}
