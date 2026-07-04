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
        equipo.Puntaje.Valor.Should().Be(0);
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

    // El puntaje del equipo es derivado: se suma a los participantes y el
    // total del equipo se recalcula como la suma de sus integrantes.
    [Fact]
    public void SumarPuntajeAParticipante_RecalculaPuntajeDelEquipo()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var lider = equipo.Participantes[0];
        var integrante = equipo.Participantes[1];

        equipo.SumarPuntajeAParticipante(lider.Id, 10);
        equipo.SumarPuntajeAParticipante(integrante.Id, 5);

        lider.Puntaje.Valor.Should().Be(10);
        integrante.Puntaje.Valor.Should().Be(5);
        equipo.Puntaje.Valor.Should().Be(15);
    }

    [Fact]
    public void SumarPuntajeAParticipante_ElEquipoSiempreEsLaSumaDeSusIntegrantes()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.SumarPuntajeAParticipante(equipo.Participantes[0].Id, 7);
        equipo.SumarPuntajeAParticipante(equipo.Participantes[1].Id, 3);
        equipo.SumarPuntajeAParticipante(equipo.Participantes[0].Id, 5);

        equipo.Puntaje.Valor.Should().Be(
            equipo.Participantes.Sum(p => p.Puntaje.Valor));
    }

    [Fact]
    public void SumarPuntajeAParticipante_NegativoLanza()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        Action accion = () => equipo.SumarPuntajeAParticipante(
            equipo.LiderParticipanteId, -1);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void SumarPuntajeAParticipante_ParticipanteAjenoLanza()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        Action accion = () => equipo.SumarPuntajeAParticipante(Guid.NewGuid(), 10);
        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
    }
}
