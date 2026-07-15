using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

// Cubre la lógica de conteo por "jugador" (participante individual o equipo)
// del repositorio: preguntas DISTINTAS (no registros), existencia de respuesta
// oficial por equipo y reanudación del progreso del equipo. Usa EF Core
// InMemory. Nota: InMemory NO aplica los índices únicos filtrados; la barrera
// real ante la carrera (requisito 19) la impone PostgreSQL con esos índices
// (ver migración CorregirUnicidadRespuestasTriviaGrupal) y su traducción a
// RespuestaTriviaDuplicadaExcepcion se cubre en las pruebas del manejador.
public class RepositorioRespuestasTriviaPruebas
{
    private static readonly Guid SesionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TriviaId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly Guid Q1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Q2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Q3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Q4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid Q5 = Guid.Parse("00000000-0000-0000-0000-000000000005");

    private static readonly Guid Ana = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid Pedro = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
    private static readonly Guid Juan = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");

    private static readonly Guid EquipoRojo = Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4");
    private static readonly Guid EquipoAzul = Guid.Parse("e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5");

    private static ContextoSesiones NuevoContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase("respuestas-" + Guid.NewGuid())
            .Options;
        return new ContextoSesiones(opciones);
    }

    private static RespuestaTriviaRegistro Registro(
        Guid preguntaId, Guid participante, Guid? equipo)
        => new(
            SesionId: SesionId,
            MisionId: MisionId,
            EtapaId: EtapaId,
            TriviaId: TriviaId,
            PreguntaId: preguntaId,
            OpcionSeleccionadaId: Guid.NewGuid(),
            ParticipanteIdentidadId: participante,
            EquipoId: equipo,
            EsCorrecta: true,
            PuntosGanados: 3,
            EventoPuntuacionId: Guid.NewGuid(),
            TiempoTardadoMs: 1000,
            FechaRespuestaUtc: DateTime.UtcNow);

    private static async Task SembrarAsync(
        IRepositorioRespuestasTrivia repo, params RespuestaTriviaRegistro[] registros)
    {
        foreach (var r in registros)
            await repo.AgregarAsync(r, CancellationToken.None);
    }

    [Fact] // (14) Registros duplicados de varios integrantes cuentan como preguntas distintas.
    public async Task ContarPreguntasDistintas_Grupal_CuentaPreguntasNoRegistros()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioRespuestasTrivia(ctx);
        // 3 registros del equipo Rojo pero sobre solo 2 preguntas distintas (Q1, Q2).
        await SembrarAsync(repo,
            Registro(Q1, Ana, EquipoRojo),
            Registro(Q1, Pedro, EquipoRojo),
            Registro(Q2, Juan, EquipoRojo));

        var distintas = await repo.ContarPreguntasDistintasDeJugadorEnEtapaAsync(
            SesionId, EtapaId, Ana, EquipoRojo, CancellationToken.None);

        distintas.Should().Be(2);
    }

    [Fact] // (7, 15) La respuesta oficial del equipo la detecta cualquier integrante.
    public async Task ExisteRespuestaOficial_Grupal_EsPorEquipo()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioRespuestasTrivia(ctx);
        await SembrarAsync(repo, Registro(Q1, Ana, EquipoRojo));

        // Pedro (mismo equipo) ya "tiene" respuesta oficial para Q1.
        (await repo.ExisteRespuestaOficialAsync(
            SesionId, EtapaId, Q1, Pedro, EquipoRojo, CancellationToken.None))
            .Should().BeTrue();

        // Otro equipo (Azul) todavía puede responder Q1.
        (await repo.ExisteRespuestaOficialAsync(
            SesionId, EtapaId, Q1, Juan, EquipoAzul, CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact] // La respuesta oficial individual es por participante.
    public async Task ExisteRespuestaOficial_Individual_EsPorParticipante()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioRespuestasTrivia(ctx);
        await SembrarAsync(repo, Registro(Q1, Ana, equipo: null));

        (await repo.ExisteRespuestaOficialAsync(
            SesionId, EtapaId, Q1, Ana, null, CancellationToken.None))
            .Should().BeTrue();
        (await repo.ExisteRespuestaOficialAsync(
            SesionId, EtapaId, Q1, Pedro, null, CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact] // (14, 16) Un equipo con registros duplicados sobre pocas preguntas NO completa.
    public async Task ContarJugadoresQueCompletaron_Grupal_SoloEquiposConTodasLasPreguntasDistintas()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioRespuestasTrivia(ctx);
        // Rojo responde las 5 preguntas distintas (aunque con integrantes repetidos).
        await SembrarAsync(repo,
            Registro(Q1, Ana, EquipoRojo),
            Registro(Q2, Pedro, EquipoRojo),
            Registro(Q3, Ana, EquipoRojo),
            Registro(Q4, Pedro, EquipoRojo),
            Registro(Q5, Juan, EquipoRojo));
        // Azul solo responde 2 preguntas distintas, pero con muchos registros duplicados.
        await SembrarAsync(repo,
            Registro(Q1, Juan, EquipoAzul),
            Registro(Q1, Ana, EquipoAzul),
            Registro(Q2, Pedro, EquipoAzul),
            Registro(Q2, Juan, EquipoAzul),
            Registro(Q2, Ana, EquipoAzul));

        var completaron = await repo.ContarJugadoresQueCompletaronEtapaAsync(
            SesionId, EtapaId, totalPreguntas: 5, CancellationToken.None);

        completaron.Should().Be(1); // solo Rojo
    }

    [Fact] // (11, reanudar) El progreso del equipo son las preguntas respondidas por el equipo.
    public async Task ObtenerPreguntasRespondidas_Grupal_DevuelveLasDelEquipo()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioRespuestasTrivia(ctx);
        await SembrarAsync(repo,
            Registro(Q1, Ana, EquipoRojo),
            Registro(Q2, Pedro, EquipoRojo),
            Registro(Q3, Juan, EquipoAzul));

        // Pedro reanuda: ve Q1 y Q2 (respondidas por su equipo Rojo), no Q3 (Azul).
        var respondidas = await repo.ObtenerPreguntasRespondidasAsync(
            SesionId, EtapaId, Pedro, EquipoRojo, CancellationToken.None);

        respondidas.Should().BeEquivalentTo(new[] { Q1, Q2 });
    }
}
