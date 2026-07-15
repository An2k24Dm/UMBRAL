using FluentAssertions;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Dominio;

// Pruebas del Aggregate Root Ranking. Cubren los ítems del alcance que son
// verificables a nivel de dominio (la unicidad por sesión y la idempotencia de
// EventoProcesado se garantizan en persistencia/infraestructura: índice UNIQUE
// en rankings.sesion_id y el repositorio de eventos procesados).
public sealed class RankingDominioPruebas
{
    // Ítem 1: se crea un Ranking para una sesión.
    [Fact]
    public void Crear_conSesion_inicializaRankingVacio()
    {
        var sesionId = Guid.NewGuid();

        var ranking = Ranking.Crear(sesionId);

        ranking.SesionId.Should().Be(sesionId);
        ranking.Id.Should().NotBe(Guid.Empty);
        ranking.Participantes.Should().BeEmpty();
        ranking.Equipos.Should().BeEmpty();
    }

    [Fact]
    public void Crear_conSesionVacia_lanzaExcepcion()
    {
        var accion = () => Ranking.Crear(Guid.Empty);

        accion.Should().Throw<RankingInvalidoExcepcion>();
    }

    // Ítem 3: se registra correctamente un participante.
    [Fact]
    public void RegistrarParticipante_agregaParticipante()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        ranking.RegistrarParticipante(participanteSesionId, identidadId, equipoId: null);

        ranking.Participantes.Should().ContainSingle();
        var p = ranking.Participantes.Single();
        p.ParticipanteSesionId.Should().Be(participanteSesionId);
        p.ParticipanteIdentidadId.Should().Be(identidadId);
        p.Puntaje.Valor.Should().Be(0);
    }

    // Ítem 4: no se duplica ParticipanteSesionId dentro del mismo Ranking.
    [Fact]
    public void RegistrarParticipante_mismoParticipanteSesionId_noDuplica()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        ranking.RegistrarParticipante(participanteSesionId, identidadId, null);
        ranking.RegistrarParticipante(participanteSesionId, identidadId, null);

        ranking.Participantes.Should().ContainSingle();
    }

    // Ítem 5: un mismo ParticipanteIdentidadId puede aparecer en distintos
    // rankings de sesiones diferentes.
    [Fact]
    public void MismaIdentidad_enRankingsDistintos_coexiste()
    {
        var identidadId = Guid.NewGuid();
        var rankingA = Ranking.Crear(Guid.NewGuid());
        var rankingB = Ranking.Crear(Guid.NewGuid());

        rankingA.RegistrarParticipante(Guid.NewGuid(), identidadId, null);
        rankingB.RegistrarParticipante(Guid.NewGuid(), identidadId, null);

        rankingA.Participantes.Single().ParticipanteIdentidadId.Should().Be(identidadId);
        rankingB.Participantes.Single().ParticipanteIdentidadId.Should().Be(identidadId);
    }

    // Ítem 6: se registra correctamente un equipo.
    [Fact]
    public void RegistrarEquipo_agregaEquipo()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();

        ranking.RegistrarEquipo(equipoId);

        ranking.Equipos.Should().ContainSingle();
        ranking.Equipos.Single().EquipoId.Should().Be(equipoId);
        ranking.Equipos.Single().Puntaje.Valor.Should().Be(0);
    }

    // Ítem 7: no se duplica EquipoId dentro del mismo Ranking.
    [Fact]
    public void RegistrarEquipo_mismoEquipoId_noDuplica()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();

        ranking.RegistrarEquipo(equipoId);
        ranking.RegistrarEquipo(equipoId);

        ranking.Equipos.Should().ContainSingle();
    }

    // Ítem 8: participante individual → EquipoId es null.
    [Fact]
    public void ParticipanteIndividual_tieneEquipoIdNull()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());

        ranking.RegistrarParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoId: null);

        ranking.Participantes.Single().EquipoId.Should().BeNull();
    }

    // Ítem 9: participante grupal → EquipoId posee valor.
    [Fact]
    public void ParticipanteGrupal_tieneEquipoIdConValor()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();

        ranking.RegistrarParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoId);

        ranking.Participantes.Single().EquipoId.Should().Be(equipoId);
    }

    // Ítem 10: se agrega puntaje correctamente a un participante.
    [Fact]
    public void RegistrarPuntajeParticipante_acumulaPuntaje()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 5);
        ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 3);

        ranking.Participantes.Single().Puntaje.Valor.Should().Be(8);
    }

    // Ítem 11: el puntaje de un equipo es igual a la suma de los puntajes de
    // sus participantes (nunca una fuente independiente que se desincronice).
    [Fact]
    public void PuntajeEquipo_esSumaDeParticipantes()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 5);
        ranking.RegistrarPuntajeParticipante(b, Guid.NewGuid(), equipoId, 5);

        ranking.Equipos.Single(e => e.EquipoId == equipoId).Puntaje.Valor.Should().Be(10);
    }

    // Ítem 11 (refuerzo): añadir más puntaje a un participante mantiene la
    // invariante (el equipo nunca queda descuadrado respecto a la suma).
    [Fact]
    public void PuntajeEquipo_semantieneComoSuma_trasVariosAportes()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 5);
        ranking.RegistrarPuntajeParticipante(b, Guid.NewGuid(), equipoId, 5);
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 4);

        var suma = ranking.Participantes.Where(p => p.EquipoId == equipoId).Sum(p => p.Puntaje.Valor);
        ranking.Equipos.Single(e => e.EquipoId == equipoId).Puntaje.Valor.Should().Be(suma);
        ranking.Equipos.Single(e => e.EquipoId == equipoId).Puntaje.Valor.Should().Be(14);
    }

    // Ítem 12: dos equipos con el mismo nombre en sesiones diferentes no
    // interfieren, porque tienen distinto EquipoId y viven en rankings distintos
    // (ranking no almacena el nombre del equipo).
    [Fact]
    public void EquiposEnSesionesDistintas_conMismoNombreConceptual_noInterfieren()
    {
        var rankingA = Ranking.Crear(Guid.NewGuid());
        var rankingB = Ranking.Crear(Guid.NewGuid());
        var equipoA = Guid.NewGuid();
        var equipoB = Guid.NewGuid();

        rankingA.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoA, 7);
        rankingB.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoB, 3);

        rankingA.Equipos.Single().Puntaje.Valor.Should().Be(7);
        rankingB.Equipos.Single().Puntaje.Valor.Should().Be(3);
        equipoA.Should().NotBe(equipoB);
    }

    // Ítem 13: dos rankings de sesiones distintas no comparten participantes ni
    // equipos accidentalmente.
    [Fact]
    public void RankingsDistintos_noCompartenEntidades()
    {
        var rankingA = Ranking.Crear(Guid.NewGuid());
        var rankingB = Ranking.Crear(Guid.NewGuid());

        rankingA.RegistrarParticipante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        rankingB.Participantes.Should().BeEmpty();
        rankingB.Equipos.Should().BeEmpty();
    }

    // Ítem 14: la posición no se persiste (las entidades no exponen Posicion).
    [Fact]
    public void EntidadesRanking_noExponenPosicion()
    {
        typeof(RankingParticipante).GetProperty("Posicion").Should().BeNull();
        typeof(RankingEquipo).GetProperty("Posicion").Should().BeNull();
    }

    // Ítem 15: la posición se calcula al consultar a partir del orden por
    // puntaje descendente.
    [Fact]
    public void ParticipantesOrdenados_ordenaPorPuntajeDescendente()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var bajo = Guid.NewGuid();
        var alto = Guid.NewGuid();
        var medio = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(bajo, Guid.NewGuid(), null, 1);
        ranking.RegistrarPuntajeParticipante(alto, Guid.NewGuid(), null, 9);
        ranking.RegistrarPuntajeParticipante(medio, Guid.NewGuid(), null, 5);

        var ordenados = ranking.ParticipantesOrdenados();

        ordenados.Select(p => p.ParticipanteSesionId)
            .Should().ContainInOrder(alto, medio, bajo);
        // La posición se derivaría como índice + 1 al proyectar.
        ordenados[0].Puntaje.Valor.Should().Be(9);
    }

    // Ítem 16: en empate se aplica una regla determinística (orden por id).
    [Fact]
    public void ParticipantesOrdenados_enEmpate_esDeterministico()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        // Se registran en orden inverso para comprobar que el desempate no
        // depende del orden de inserción.
        ranking.RegistrarPuntajeParticipante(id2, Guid.NewGuid(), null, 5);
        ranking.RegistrarPuntajeParticipante(id1, Guid.NewGuid(), null, 5);

        var ordenados = ranking.ParticipantesOrdenados();

        ordenados.Select(p => p.ParticipanteSesionId).Should().ContainInOrder(id1, id2);
    }

    // Ítem 17: el ranking global agrupa por ParticipanteIdentidadId y suma
    // puntajes de distintas sesiones, sin ninguna entidad global persistida.
    [Fact]
    public void RankingGlobal_agrupaPorIdentidadYSuma()
    {
        var identidad = Guid.NewGuid();
        var rankingA = Ranking.Crear(Guid.NewGuid());
        var rankingB = Ranking.Crear(Guid.NewGuid());
        var rankingC = Ranking.Crear(Guid.NewGuid());
        rankingA.RegistrarPuntajeParticipante(Guid.NewGuid(), identidad, null, 10);
        rankingB.RegistrarPuntajeParticipante(Guid.NewGuid(), identidad, null, 20);
        rankingC.RegistrarPuntajeParticipante(Guid.NewGuid(), identidad, null, 5);

        // Proyección equivalente a GROUP BY ParticipanteIdentidadId + SUM.
        var totalGlobal = new[] { rankingA, rankingB, rankingC }
            .SelectMany(r => r.Participantes)
            .Where(p => p.ParticipanteIdentidadId == identidad)
            .Sum(p => p.Puntaje.Valor);

        totalGlobal.Should().Be(35);
    }

    // Ítem 19: el detalle de un equipo devuelve los aportes individuales.
    [Fact]
    public void ParticipantesDeEquipo_devuelveAportesIndividuales()
    {
        var ranking = Ranking.Crear(Guid.NewGuid());
        var equipoId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var ajeno = Guid.NewGuid();
        ranking.RegistrarPuntajeParticipante(a, Guid.NewGuid(), equipoId, 6);
        ranking.RegistrarPuntajeParticipante(b, Guid.NewGuid(), equipoId, 4);
        ranking.RegistrarPuntajeParticipante(ajeno, Guid.NewGuid(), Guid.NewGuid(), 8);

        var aportes = ranking.ParticipantesDeEquipo(equipoId);

        aportes.Should().HaveCount(2);
        aportes.Sum(p => p.Puntaje.Valor).Should().Be(10);
        aportes.Select(p => p.ParticipanteSesionId).Should().BeEquivalentTo(new[] { a, b });
    }

    // El Value Object Puntaje no admite valores ni deltas negativos (misma
    // invariante que PuntajeSesion en sesiones-servicio).
    [Fact]
    public void Puntaje_noPermiteNegativos()
    {
        var accionDesde = () => Puntaje.Desde(-1);
        var accionSumar = () => Puntaje.Cero.Sumar(-5);

        accionDesde.Should().Throw<RankingInvalidoExcepcion>();
        accionSumar.Should().Throw<RankingInvalidoExcepcion>();
    }
}
