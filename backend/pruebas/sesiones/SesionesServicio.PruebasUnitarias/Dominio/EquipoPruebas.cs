using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class EquipoPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const int MaximoParticipantesPorEquipo = 2;

    private static SesionGrupal CrearSesion()
        => SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: MaximoParticipantesPorEquipo);

    [Fact]
    public void CrearEquipo_LiderEsIntegrante()
    {
        var sesion = CrearSesion();
        var lider = Guid.NewGuid();
        var equipo = sesion.CrearEquipo("Rojo", lider, AhoraUtc, AhoraUtc);

        equipo.Participantes.Should().ContainSingle(
            p => p.ParticipanteIdentidadId == lider);
        equipo.LiderParticipanteId.Should().Be(equipo.Participantes[0].Id);
    }

    [Fact]
    public void EquipoNace_ConPuntajeCero()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.Puntaje.Should().Be(0);
    }

    [Fact]
    public void EstaLleno_RefleyaCapacidad()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.EstaLleno().Should().BeFalse();

        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.EstaLleno().Should().BeTrue();
    }

    [Fact]
    public void SumarPuntaje_AcumulaEnEquipo()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.SumarPuntaje(10);
        equipo.Puntaje.Should().Be(10);
    }

    [Fact]
    public void SumarPuntaje_NegativoLanza()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        Action accion = () => equipo.SumarPuntaje(-1);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }
}
