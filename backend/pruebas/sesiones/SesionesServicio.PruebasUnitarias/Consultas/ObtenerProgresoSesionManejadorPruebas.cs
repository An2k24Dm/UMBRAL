using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;
using SesionGrupalEntidad = SesionesServicio.Dominio.Entidades.SesionGrupal;
using SesionIndividualEntidad = SesionesServicio.Dominio.Entidades.SesionIndividual;

namespace SesionesServicio.PruebasUnitarias.Consultas;

public class ObtenerProgresoSesionManejadorPruebas
{
    private static readonly Guid SesionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Participante1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Participante2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime AhoraUtc = new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);

    private static ObtenerProgresoSesionManejador Construir(
        IReadOnlyList<ProgresoTriviaItem>? triviaItems = null,
        IReadOnlyList<ProgresoTesoroItem>? tesoroItems = null,
        IReadOnlyList<ProgresoTriviaEtapaItem>? triviaEtapasItems = null,
        Sesion? sesion = null,
        IDictionary<Guid, int>? preguntasPorTrivia = null,
        Mock<IClienteJuegosTrivia>? clienteTriviaMock = null)
    {
        var repoSesiones = new Mock<IRepositorioSesiones>();
        repoSesiones.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion ?? SesionIndividual());

        var repoTrivia = new Mock<IRepositorioRespuestasTrivia>();
        repoTrivia.Setup(r => r.ObtenerProgresoTriviaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triviaItems ?? Array.Empty<ProgresoTriviaItem>());
        repoTrivia.Setup(r => r.ObtenerProgresoTriviaPorEtapaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triviaEtapasItems ?? Array.Empty<ProgresoTriviaEtapaItem>());

        var repoTesoro = new Mock<IRepositorioEvidenciasTesoro>();
        repoTesoro.Setup(r => r.ObtenerProgresoTesoroAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tesoroItems ?? Array.Empty<ProgresoTesoroItem>());

        var clienteTrivia = clienteTriviaMock ?? new Mock<IClienteJuegosTrivia>();
        foreach (var par in preguntasPorTrivia ?? new Dictionary<Guid, int>())
        {
            clienteTrivia.Setup(c => c.ObtenerTriviaParticipanteAsync(
                    par.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TriviaConPreguntas(par.Value));
        }
        var finalizacion = new Mock<IServicioFinalizacionSesion>();
        finalizacion.Setup(f => f.FinalizarSesionSiDuracionVencidaAsync(
                SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return new ObtenerProgresoSesionManejador(
            repoSesiones.Object,
            repoTrivia.Object,
            repoTesoro.Object,
            clienteTrivia.Object,
            finalizacion.Object);
    }

    private static async Task<List<ProgresoSesionParticipanteDto>> EjecutarFilas(
        IReadOnlyList<ProgresoTriviaItem>? trivia = null,
        IReadOnlyList<ProgresoTesoroItem>? tesoro = null,
        IReadOnlyList<ProgresoTriviaEtapaItem>? triviaEtapas = null,
        Sesion? sesion = null,
        IDictionary<Guid, int>? preguntasPorTrivia = null)
    {
        var resultado = await Construir(
                trivia, tesoro, triviaEtapas, sesion, preguntasPorTrivia)
            .Handle(new ObtenerProgresoSesionConsulta(SesionId), CancellationToken.None);
        return resultado.Filas;
    }

    private static Task<ProgresoSesionDto> Ejecutar(
        IReadOnlyList<ProgresoTriviaItem>? trivia = null,
        IReadOnlyList<ProgresoTesoroItem>? tesoro = null,
        IReadOnlyList<ProgresoTriviaEtapaItem>? triviaEtapas = null,
        Sesion? sesion = null,
        IDictionary<Guid, int>? preguntasPorTrivia = null,
        Mock<IClienteJuegosTrivia>? clienteTriviaMock = null)
        => Construir(
                trivia, tesoro, triviaEtapas, sesion, preguntasPorTrivia,
                clienteTriviaMock)
            .Handle(new ObtenerProgresoSesionConsulta(SesionId), CancellationToken.None);

    [Fact]
    public async Task SinRespuestasNiEvidencias_DevuelveListaVacia()
    {
        var resultado = await EjecutarFilas();

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task SoloTrivia_DevuelveProgresoTrivia()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 5, Correctas: 3, PuntosGanados: 80)
        };

        var resultado = await EjecutarFilas(trivia: triviaItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.ParticipanteIdentidadId.Should().Be(Participante1);
        item.TriviaRespondidas.Should().Be(5);
        item.TriviaCorrectas.Should().Be(3);
        item.TriviaIncorrectas.Should().Be(2);
        item.TesoroIntentosEnviados.Should().Be(0);
        item.TesoroEtapasCompletadas.Should().Be(0);
    }

    [Fact]
    public async Task SoloTesoro_DevuelveProgresoTesoro()
    {
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante1, TotalIntentados: 3, Validos: 2, PuntosGanados: 50)
        };

        var resultado = await EjecutarFilas(tesoro: tesoroItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.ParticipanteIdentidadId.Should().Be(Participante1);
        item.TriviaRespondidas.Should().Be(0);
        item.TesoroIntentosEnviados.Should().Be(3);
        item.TesoroEtapasCompletadas.Should().Be(2);
    }

    [Fact]
    public async Task TriviaYTesoro_MismoParticipante_CombinaProgreso()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 4, Correctas: 4, PuntosGanados: 100)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante1, TotalIntentados: 2, Validos: 1, PuntosGanados: 60)
        };

        var resultado = await EjecutarFilas(trivia: triviaItems, tesoro: tesoroItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.TriviaRespondidas.Should().Be(4);
        item.TesoroIntentosEnviados.Should().Be(2);
    }

    [Fact]
    public async Task TriviaYTesoro_MismoEquipo_AgrupaProgresoPorEquipo()
    {
        var equipoId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, equipoId, TotalRespondidas: 2, Correctas: 1, PuntosGanados: 40)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante2, equipoId, TotalIntentados: 1, Validos: 1, PuntosGanados: 60)
        };

        var resultado = await EjecutarFilas(trivia: triviaItems, tesoro: tesoroItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.EquipoId.Should().Be(equipoId);
        item.TriviaRespondidas.Should().Be(2);
        item.TesoroIntentosEnviados.Should().Be(1);
    }

    [Fact]
    public async Task ParticipantesSoloEnUno_OtroTipoEsCero()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 3, Correctas: 2, PuntosGanados: 40)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante2, TotalIntentados: 1, Validos: 1, PuntosGanados: 25)
        };

        var resultado = await EjecutarFilas(trivia: triviaItems, tesoro: tesoroItems);

        resultado.Should().HaveCount(2);

        var p1 = resultado.Single(x => x.ParticipanteIdentidadId == Participante1);
        p1.TriviaRespondidas.Should().Be(3);
        p1.TesoroIntentosEnviados.Should().Be(0);

        var p2 = resultado.Single(x => x.ParticipanteIdentidadId == Participante2);
        p2.TriviaRespondidas.Should().Be(0);
        p2.TesoroIntentosEnviados.Should().Be(1);
    }

    [Fact]
    public async Task MultipleParticipantesEnTrivia_DevuelveUnoPorParticipante()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 5, Correctas: 5, PuntosGanados: 150),
            new(Participante2, TotalRespondidas: 5, Correctas: 3, PuntosGanados: 90)
        };

        var resultado = await EjecutarFilas(trivia: triviaItems);

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(x => x.ParticipanteIdentidadId == Participante1 && x.TriviaRespondidas == 5);
        resultado.Should().Contain(x => x.ParticipanteIdentidadId == Participante2 && x.TriviaCorrectas == 3);
    }

    [Fact]
    public async Task IncorrectasCalculadasComoRespondidas_MenosCorrectas()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 10, Correctas: 7, PuntosGanados: 200)
        };

        var resultado = await EjecutarFilas(trivia: triviaItems);

        resultado[0].TriviaIncorrectas.Should().Be(3);
    }

    [Fact]
    public async Task Individual_TriviaIncompleta_NoCuentaEtapaCompletada()
    {
        var etapaId = Guid.NewGuid();
        var triviaId = Guid.NewGuid();
        var sesion = SesionIndividual(
            EtapaTrivia(etapaId, triviaId, ordenMision: 1, ordenEtapa: 1));
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 3, Correctas: 2, PuntosGanados: 20)
        };
        var triviaEtapas = new List<ProgresoTriviaEtapaItem>
        {
            new(Participante1, null, etapaId, PreguntasRespondidas: 3)
        };

        var resultado = await EjecutarFilas(
            trivia: triviaItems,
            triviaEtapas: triviaEtapas,
            sesion: sesion,
            preguntasPorTrivia: new Dictionary<Guid, int> { [triviaId] = 5 });

        resultado.Single().TriviaEtapasCompletadas.Should().Be(0);
    }

    [Fact]
    public async Task Individual_TriviaCompleta_CuentaEtapaYConservaCorrectasIncorrectas()
    {
        var etapaId = Guid.NewGuid();
        var triviaId = Guid.NewGuid();
        var sesion = SesionIndividual(
            EtapaTrivia(etapaId, triviaId, ordenMision: 1, ordenEtapa: 1));
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 7, Correctas: 5, PuntosGanados: 50)
        };
        var triviaEtapas = new List<ProgresoTriviaEtapaItem>
        {
            new(Participante1, null, etapaId, PreguntasRespondidas: 5)
        };

        var resultado = await EjecutarFilas(
            trivia: triviaItems,
            triviaEtapas: triviaEtapas,
            sesion: sesion,
            preguntasPorTrivia: new Dictionary<Guid, int> { [triviaId] = 5 });

        var fila = resultado.Single();
        fila.TriviaEtapasCompletadas.Should().Be(1);
        fila.TriviaCorrectas.Should().Be(5);
        fila.TriviaIncorrectas.Should().Be(2);
    }

    [Fact]
    public async Task Individual_TesoroValido_CuentaEtapaCompletada()
    {
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante1, TotalIntentados: 2, Validos: 1, PuntosGanados: 40)
        };

        var resultado = await EjecutarFilas(tesoro: tesoroItems);

        resultado.Single().TesoroEtapasCompletadas.Should().Be(1);
    }

    [Fact]
    public async Task Grupal_TriviaCompleta_AgrupaPorEquipoSinFilaPorIntegrante()
    {
        var equipoId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var triviaId = Guid.NewGuid();
        var sesion = SesionGrupal(
            EtapaTrivia(etapaId, triviaId, ordenMision: 1, ordenEtapa: 1));
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, equipoId, TotalRespondidas: 5, Correctas: 4, PuntosGanados: 40)
        };
        var triviaEtapas = new List<ProgresoTriviaEtapaItem>
        {
            new(Participante1, equipoId, etapaId, PreguntasRespondidas: 5)
        };

        var resultado = await EjecutarFilas(
            trivia: triviaItems,
            triviaEtapas: triviaEtapas,
            sesion: sesion,
            preguntasPorTrivia: new Dictionary<Guid, int> { [triviaId] = 5 });

        resultado.Should().ContainSingle();
        resultado.Single().EquipoId.Should().Be(equipoId);
        resultado.Single().TriviaEtapasCompletadas.Should().Be(1);
    }

    [Fact]
    public async Task DosEquipos_NoMezclaMetricas()
    {
        var equipoA = Guid.NewGuid();
        var equipoB = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var triviaId = Guid.NewGuid();
        var sesion = SesionGrupal(
            EtapaTrivia(etapaId, triviaId, ordenMision: 1, ordenEtapa: 1));
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, equipoA, TotalRespondidas: 7, Correctas: 5, PuntosGanados: 50),
            new(Participante2, equipoB, TotalRespondidas: 7, Correctas: 3, PuntosGanados: 30)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante1, equipoA, TotalIntentados: 1, Validos: 1, PuntosGanados: 20),
            new(Participante2, equipoB, TotalIntentados: 0, Validos: 0, PuntosGanados: 0)
        };
        var triviaEtapas = new List<ProgresoTriviaEtapaItem>
        {
            new(Participante1, equipoA, etapaId, PreguntasRespondidas: 5),
            new(Participante2, equipoB, etapaId, PreguntasRespondidas: 4)
        };

        var resultado = await EjecutarFilas(
            trivia: triviaItems,
            tesoro: tesoroItems,
            triviaEtapas: triviaEtapas,
            sesion: sesion,
            preguntasPorTrivia: new Dictionary<Guid, int> { [triviaId] = 5 });

        var filaA = resultado.Single(x => x.EquipoId == equipoA);
        filaA.TriviaEtapasCompletadas.Should().Be(1);
        filaA.TriviaCorrectas.Should().Be(5);
        filaA.TriviaIncorrectas.Should().Be(2);
        filaA.TesoroEtapasCompletadas.Should().Be(1);

        var filaB = resultado.Single(x => x.EquipoId == equipoB);
        filaB.TriviaEtapasCompletadas.Should().Be(0);
        filaB.TriviaCorrectas.Should().Be(3);
        filaB.TriviaIncorrectas.Should().Be(4);
        filaB.TesoroEtapasCompletadas.Should().Be(0);
    }

    [Fact]
    public async Task UbicacionActual_SaleDeEjecucionActual()
    {
        var etapa = EtapaTrivia(Guid.NewGuid(), Guid.NewGuid(), ordenMision: 1, ordenEtapa: 2);
        var sesion = SesionIndividual(new[] { etapa }, ejecucionActual: etapa);

        var resultado = await Ejecutar(sesion: sesion);

        resultado.OrdenMisionActual.Should().Be(1);
        resultado.OrdenEtapaActual.Should().Be(2);
        resultado.MisionActualId.Should().Be(etapa.MisionId);
        resultado.EtapaActualId.Should().Be(etapa.EtapaId);
    }

    [Fact]
    public async Task CambioDeEtapa_ActualizaUbicacionYConservaMetricasAcumuladas()
    {
        var triviaId = Guid.NewGuid();
        var etapa1 = EtapaTrivia(Guid.NewGuid(), triviaId, ordenMision: 1, ordenEtapa: 1);
        var etapa2 = EtapaTesoro(Guid.NewGuid(), Guid.NewGuid(), ordenMision: 1, ordenEtapa: 2);
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 5, Correctas: 5, PuntosGanados: 50)
        };
        var triviaEtapas = new List<ProgresoTriviaEtapaItem>
        {
            new(Participante1, null, etapa1.EtapaId, PreguntasRespondidas: 5)
        };
        var preguntas = new Dictionary<Guid, int> { [triviaId] = 5 };

        var antes = await Ejecutar(
            trivia: triviaItems,
            triviaEtapas: triviaEtapas,
            sesion: SesionIndividual(new[] { etapa1, etapa2 }, etapa1),
            preguntasPorTrivia: preguntas);
        var despues = await Ejecutar(
            trivia: triviaItems,
            triviaEtapas: triviaEtapas,
            sesion: SesionIndividual(new[] { etapa1, etapa2 }, etapa2),
            preguntasPorTrivia: preguntas);

        antes.OrdenEtapaActual.Should().Be(1);
        despues.OrdenEtapaActual.Should().Be(2);
        despues.Filas.Single().TriviaEtapasCompletadas.Should().Be(1);
        despues.Filas.Single().TriviaCorrectas.Should().Be(5);
    }

    [Fact]
    public async Task TotalPreguntasTrivia_SeConsultaPorEtapaNoPorFila()
    {
        var equipoA = Guid.NewGuid();
        var equipoB = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var triviaId = Guid.NewGuid();
        var clienteTrivia = new Mock<IClienteJuegosTrivia>();
        clienteTrivia.Setup(c => c.ObtenerTriviaParticipanteAsync(
                triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriviaConPreguntas(5));

        await Ejecutar(
            trivia: new[]
            {
                new ProgresoTriviaItem(Participante1, equipoA, 5, 5, 50),
                new ProgresoTriviaItem(Participante2, equipoB, 5, 3, 30)
            },
            triviaEtapas: new[]
            {
                new ProgresoTriviaEtapaItem(Participante1, equipoA, etapaId, 5),
                new ProgresoTriviaEtapaItem(Participante2, equipoB, etapaId, 5)
            },
            sesion: SesionGrupal(EtapaTrivia(etapaId, triviaId, 1, 1)),
            clienteTriviaMock: clienteTrivia);

        clienteTrivia.Verify(c => c.ObtenerTriviaParticipanteAsync(
            triviaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static TriviaParticipanteJuegosDto TriviaConPreguntas(int total)
        => new()
        {
            Id = Guid.NewGuid(),
            Preguntas = Enumerable.Range(0, total)
                .Select(_ => new PreguntaParticipanteJuegosDto { Id = Guid.NewGuid() })
                .ToList()
        };

    private static EjecucionActualSesion EtapaTrivia(
        Guid etapaId, Guid triviaId, int ordenMision, int ordenEtapa)
        => EjecucionActualSesion.Rehidratar(
            Guid.NewGuid(),
            etapaId,
            triviaId,
            "Trivia",
            ordenGlobal: ordenEtapa,
            fechaInicioUtc: AhoraUtc,
            duracionSegundos: 60,
            duracionPausasAcumuladaMs: 0,
            fechaInicioPausaUtc: null,
            fase: FaseEjecucionEtapaSesion.Activa,
            ordenMision: ordenMision,
            ordenEtapa: ordenEtapa);

    private static EjecucionActualSesion EtapaTesoro(
        Guid etapaId, Guid busquedaId, int ordenMision, int ordenEtapa)
        => EjecucionActualSesion.Rehidratar(
            Guid.NewGuid(),
            etapaId,
            busquedaId,
            "BusquedaTesoro",
            ordenGlobal: ordenEtapa,
            fechaInicioUtc: AhoraUtc,
            duracionSegundos: 60,
            duracionPausasAcumuladaMs: 0,
            fechaInicioPausaUtc: null,
            fase: FaseEjecucionEtapaSesion.Activa,
            ordenMision: ordenMision,
            ordenEtapa: ordenEtapa);

    private static SesionIndividualEntidad SesionIndividual(
        params EjecucionActualSesion[] secuencia)
        => SesionIndividual(secuencia, secuencia.FirstOrDefault());

    private static SesionIndividualEntidad SesionIndividual(
        IEnumerable<EjecucionActualSesion> secuencia,
        EjecucionActualSesion? ejecucionActual)
        => SesionIndividualEntidad.Rehidratar(
            SesionId,
            "Sesion",
            "Demo",
            EstadoSesion.Activa,
            AhoraUtc,
            "ABC123",
            Operador,
            AhoraUtc,
            AhoraUtc,
            null,
            10,
            ejecucionActual: ejecucionActual,
            secuenciaEtapas: secuencia);

    private static SesionGrupalEntidad SesionGrupal(
        params EjecucionActualSesion[] secuencia)
        => SesionGrupalEntidad.Rehidratar(
            SesionId,
            "Sesion",
            "Demo",
            EstadoSesion.Activa,
            AhoraUtc,
            "ABC123",
            Operador,
            AhoraUtc,
            AhoraUtc,
            null,
            5,
            3,
            ejecucionActual: secuencia.FirstOrDefault(),
            secuenciaEtapas: secuencia);
}
