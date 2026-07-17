using System;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class SnapshotRankingDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 16, 14, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Participante_AplicaSnapshotMasReciente_YRechazaSnapshotAntiguo()
    {
        var participante = Participante.CrearParaSesionIndividual(
            Guid.NewGuid(), Guid.NewGuid(), AhoraUtc);

        var primero = participante.EstablecerPuntajeSnapshot(100, AhoraUtc);
        var antiguo = participante.EstablecerPuntajeSnapshot(50, AhoraUtc.AddSeconds(-1));

        primero.Should().BeTrue();
        antiguo.Should().BeFalse();
        participante.Puntaje.Valor.Should().Be(100);
        participante.SnapshotRankingUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public void Equipo_AplicaSnapshotConPuntajeEquipoPersistido()
    {
        var sesion = CrearSesionGrupal();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var lider = equipo.Participantes[0];

        var actualizado = equipo.EstablecerPuntajeSnapshotParticipante(
            lider.Id, 25, 90, AhoraUtc);

        actualizado.Should().BeTrue();
        lider.Puntaje.Valor.Should().Be(25);
        equipo.Puntaje.Valor.Should().Be(90);
        equipo.SnapshotRankingUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public void Equipo_SinPuntajeEquipo_RecalculaDesdeIntegrantes()
    {
        var sesion = CrearSesionGrupal();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var lider = equipo.Participantes[0];
        var integrante = equipo.Participantes[1];
        equipo.SumarPuntajeAParticipante(integrante.Id, 10);

        equipo.EstablecerPuntajeSnapshotParticipante(lider.Id, 40, null, AhoraUtc)
            .Should().BeTrue();

        equipo.Puntaje.Valor.Should().Be(50);
    }

    [Fact]
    public void Equipo_SnapshotAntiguo_NoActualizaEquipoNiParticipante()
    {
        var sesion = CrearSesionGrupal();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var lider = equipo.Participantes[0];
        equipo.EstablecerPuntajeSnapshotParticipante(lider.Id, 30, 60, AhoraUtc);

        var actualizado = equipo.EstablecerPuntajeSnapshotParticipante(
            lider.Id, 10, 20, AhoraUtc.AddSeconds(-1));

        actualizado.Should().BeFalse();
        lider.Puntaje.Valor.Should().Be(30);
        equipo.Puntaje.Valor.Should().Be(60);
        equipo.SnapshotRankingUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public void Equipo_SnapshotMismaFecha_NoActualizaEquipo()
    {
        var sesion = CrearSesionGrupal();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var lider = equipo.Participantes[0];
        equipo.EstablecerPuntajeSnapshotParticipante(lider.Id, 30, 60, AhoraUtc);

        var actualizado = equipo.EstablecerPuntajeSnapshotParticipante(
            lider.Id, 80, 120, AhoraUtc);

        actualizado.Should().BeFalse();
        lider.Puntaje.Valor.Should().Be(30);
        equipo.Puntaje.Valor.Should().Be(60);
    }

    [Fact]
    public void Equipo_SnapshotSimple_SinFecha_ActualizaParticipanteYRecalculaEquipo()
    {
        var sesion = CrearSesionGrupal();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.EstablecerPuntajeSnapshotParticipante(equipo.Participantes[0].Id, 15, null);
        equipo.EstablecerPuntajeSnapshotParticipante(equipo.Participantes[1].Id, 20, null);

        equipo.Puntaje.Valor.Should().Be(35);
        equipo.SnapshotRankingUtc.Should().BeNull();
    }

    private static SesionGrupal CrearSesionGrupal()
        => SesionGrupal.Crear(
            "Sesión",
            "Demo",
            AhoraUtc.AddHours(1),
            "ABC123",
            Operador,
            AhoraUtc,
            maximoEquipos: 3,
            maximoParticipantesPorEquipo: 3);
}
