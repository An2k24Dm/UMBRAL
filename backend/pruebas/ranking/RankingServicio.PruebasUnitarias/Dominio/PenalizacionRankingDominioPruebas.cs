using FluentAssertions;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Dominio;

// HU52 — Reglas de dominio de penalización en Ranking: CantidadPenalizacion,
// puntaje acumulado negativo, penalización individual y grupal, conservación de
// la penalización del equipo tras recálculos y orden con negativos.
public sealed class PenalizacionRankingDominioPruebas
{
    // CantidadPenalizacion acepta los límites 1 y 100.
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(50)]
    public void CantidadPenalizacion_aceptaRangoValido(int valor)
    {
        var cantidad = CantidadPenalizacion.Crear(valor);

        cantidad.Valor.Should().Be(valor);
    }

    // CantidadPenalizacion rechaza 0, negativos y > 100.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(101)]
    [InlineData(500)]
    public void CantidadPenalizacion_rechazaFueraDeRango(int valor)
    {
        var accion = () => CantidadPenalizacion.Crear(valor);

        accion.Should().Throw<RankingInvalidoExcepcion>();
    }

    // El puntaje acumulado puede quedar negativo tras una penalización.
    [Fact]
    public void Puntaje_aplicarPenalizacion_puedeQuedarNegativo()
    {
        var puntaje = Puntaje.Cero.AplicarPenalizacion(CantidadPenalizacion.Crear(5));

        puntaje.Valor.Should().Be(-5);
    }

    // DesdePersistencia admite negativos (rehidratación de acumulados < 0).
    [Fact]
    public void Puntaje_desdePersistencia_admiteNegativos()
    {
        Puntaje.DesdePersistencia(-13).Valor.Should().Be(-13);
    }

    // El puntaje ganado sigue sin admitir negativos.
    [Fact]
    public void Puntaje_ganado_sigueProhibiendoNegativos()
    {
        var accionDesde = () => Puntaje.Desde(-1);
        var accionSumar = () => Puntaje.Cero.Sumar(-5);

        accionDesde.Should().Throw<RankingInvalidoExcepcion>();
        accionSumar.Should().Throw<RankingInvalidoExcepcion>();
    }

    // Penalización individual: descuenta del participante y acumula la magnitud.
    [Fact]
    public void PenalizacionIndividual_descuentaYAcumula()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 3);

        ranking.AplicarPenalizacionParticipante(
            participanteSesionId, identidadId, CantidadPenalizacion.Crear(5));

        var participante = ranking.Participantes.Single();
        participante.Puntaje.Valor.Should().Be(-2);
        participante.PuntosPenalizados.Should().Be(5);
    }

    // Ejemplo del enunciado: puntaje 0, penalización 5 → nuevo puntaje -5.
    [Fact]
    public void PenalizacionIndividual_sobreCero_quedaNegativo()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        ranking.RegistrarParticipante(participanteSesionId, identidadId, null);

        ranking.AplicarPenalizacionParticipante(
            participanteSesionId, identidadId, CantidadPenalizacion.Crear(5));

        ranking.Participantes.Single().Puntaje.Valor.Should().Be(-5);
    }

    // Múltiples penalizaciones individuales se acumulan (magnitud y descuento).
    [Fact]
    public void PenalizacionIndividual_multiple_acumula()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 20);

        ranking.AplicarPenalizacionParticipante(participanteSesionId, identidadId, CantidadPenalizacion.Crear(5));
        ranking.AplicarPenalizacionParticipante(participanteSesionId, identidadId, CantidadPenalizacion.Crear(8));

        var participante = ranking.Participantes.Single();
        participante.PuntosPenalizados.Should().Be(13);
        participante.Puntaje.Valor.Should().Be(7);
    }

    // Penalización individual no modifica a otro participante.
    [Fact]
    public void PenalizacionIndividual_noModificaOtroParticipante()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var objetivo = Guid.NewGuid();
        var otro = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(objetivo, Guid.NewGuid(), null, 10);
        ranking.RegistrarPuntajeParticipante(otro, Guid.NewGuid(), null, 10);

        ranking.AplicarPenalizacionParticipante(objetivo, Guid.NewGuid(), CantidadPenalizacion.Crear(4));

        ranking.Participantes.Single(p => p.ParticipanteSesionId == otro)
            .Puntaje.Valor.Should().Be(10);
        ranking.Participantes.Single(p => p.ParticipanteSesionId == otro)
            .PuntosPenalizados.Should().Be(0);
    }

    // Penalización grupal: afecta al equipo y no a los participantes.
    [Fact]
    public void PenalizacionGrupal_afectaEquipoNoParticipantes()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 50);
        ranking.RegistrarPuntajeParticipante(b, Guid.NewGuid(), equipoId, 30);

        ranking.AplicarPenalizacionEquipo(equipoId, CantidadPenalizacion.Crear(20));

        var equipo = ranking.Equipos.Single(e => e.EquipoId == equipoId);
        equipo.Puntaje.Valor.Should().Be(60);
        equipo.PuntosPenalizados.Should().Be(20);
        // Los aportes individuales no cambian.
        ranking.Participantes.Single(p => p.ParticipanteSesionId == a).Puntaje.Valor.Should().Be(50);
        ranking.Participantes.Single(p => p.ParticipanteSesionId == b).Puntaje.Valor.Should().Be(30);
        ranking.Participantes.All(p => p.PuntosPenalizados == 0).Should().BeTrue();
    }

    // El puntaje del equipo puede quedar negativo.
    [Fact]
    public void PenalizacionGrupal_puedeQuedarNegativo()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoId, 10);

        ranking.AplicarPenalizacionEquipo(equipoId, CantidadPenalizacion.Crear(30));

        ranking.Equipos.Single(e => e.EquipoId == equipoId).Puntaje.Valor.Should().Be(-20);
    }

    // Escenario obligatorio: el equipo conserva la penalización tras un puntaje
    // ganado posterior. total = suma de participantes − penalizaciones.
    [Fact]
    public void PenalizacionGrupal_seConservaTrasPuntajePosterior()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 20);

        ranking.AplicarPenalizacionEquipo(equipoId, CantidadPenalizacion.Crear(10));
        // Después un participante gana puntos; el recálculo no borra la penalización.
        ranking.RegistrarPuntajeParticipante(b, Guid.NewGuid(), equipoId, 30);

        var equipo = ranking.Equipos.Single(e => e.EquipoId == equipoId);
        equipo.PuntosPenalizados.Should().Be(10);
        equipo.Puntaje.Valor.Should().Be(40); // (20 + 30) − 10
    }

    // El equipo conserva varias penalizaciones tras múltiples recálculos.
    [Fact]
    public void PenalizacionGrupal_conservaVariasTrasRecalculos()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        var a = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 50);

        ranking.AplicarPenalizacionEquipo(equipoId, CantidadPenalizacion.Crear(10));
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 10);
        ranking.AplicarPenalizacionEquipo(equipoId, CantidadPenalizacion.Crear(5));
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 10);

        var equipo = ranking.Equipos.Single(e => e.EquipoId == equipoId);
        equipo.PuntosPenalizados.Should().Be(15);
        equipo.Puntaje.Valor.Should().Be(55); // 70 − 15
    }

    // El orden funciona con puntajes negativos.
    [Fact]
    public void Orden_conNegativos_ordenaCorrectamente()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var positivo = Guid.NewGuid();
        var negativo = Guid.NewGuid();
        var cero = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(cero, Guid.NewGuid(), null, 0);
        ranking.RegistrarPuntajeParticipante(positivo, Guid.NewGuid(), null, 5);
        ranking.RegistrarPuntajeParticipante(negativo, Guid.NewGuid(), null, 2);
        ranking.AplicarPenalizacionParticipante(negativo, Guid.NewGuid(), CantidadPenalizacion.Crear(10));

        var ordenados = ranking.ParticipantesOrdenados();

        ordenados.Select(p => p.ParticipanteSesionId)
            .Should().ContainInOrder(positivo, cero, negativo);
        ordenados.Last().Puntaje.Valor.Should().Be(-8);
    }
}
