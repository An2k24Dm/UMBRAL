using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.PruebasUnitarias.Servicios;

public class ServicioProgresoSecuencialSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OperadorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MisionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MisionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid EtapaTesoro = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EtapaTrivia = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid BusquedaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TriviaId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private sealed class Contexto
    {
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IRepositorioSesiones> RepositorioSesiones { get; } = new();
        public Mock<IClienteJuegosMisiones> ClienteMisiones { get; } = new();
        public Mock<IClienteJuegosTrivia> ClienteTrivia { get; } = new();
        public Mock<IRepositorioRespuestasTrivia> RespuestasTrivia { get; } = new();
        public Mock<IRepositorioEvidenciasTesoro> EvidenciasTesoro { get; } = new();
        public Mock<IRepositorioEtapasCompletadas> EtapasCompletadas { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Mock<IServicioFinalizacionSesion> Finalizacion { get; } = new();
        public SesionIndividual Sesion { get; }

        public Contexto()
        {
            Sesion = SesionIndividual.Crear(
                "Sesion", "Demo", AhoraUtc.AddHours(1), "SEQ01", OperadorId, AhoraUtc, 5);
            Sesion.AsignarMisiones(new[] { MisionA, MisionB });
            Sesion.Preparar();
            Sesion.AgregarParticipante(ParticipanteId, AhoraUtc);
            Sesion.IniciarPrimeraEtapa(
                MisionA, EtapaTesoro, BusquedaId, "BusquedaTesoro", 1, AhoraUtc, 300);

            Usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            RepositorioSesiones.Setup(r => r.ObtenerPorIdAsync(Sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Sesion);
            EtapasCompletadas.Setup(r => r.ObtenerCompletadasAsync(
                    Sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Guid>());

            // El progreso de Trivia reconstruye el reloj por preguntas a partir de
            // las respuestas oficiales del jugador; por defecto no hay ninguna.
            RespuestasTrivia.Setup(r => r.ObtenerRespuestasConTiempoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RespuestaTriviaTiempo>());

            ClienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(MisionA, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionConEtapasJuegosDto
                {
                    Id = MisionA,
                    Etapas =
                    {
                        new EtapaJuegosDto
                        {
                            Id = EtapaTesoro,
                            Orden = 1,
                            TipoModoDeJuego = "BusquedaTesoro",
                            ModoDeJuegoId = BusquedaId
                        }
                    }
                });

            ClienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(MisionB, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionConEtapasJuegosDto
                {
                    Id = MisionB,
                    Etapas =
                    {
                        new EtapaJuegosDto
                        {
                            Id = EtapaTrivia,
                            Orden = 1,
                            TipoModoDeJuego = "Trivia",
                            ModoDeJuegoId = TriviaId
                        }
                    }
                });

            ClienteTrivia.Setup(c => c.ObtenerTriviaParticipanteAsync(TriviaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TriviaParticipanteJuegosDto
                {
                    Id = TriviaId,
                    Preguntas =
                    {
                        new PreguntaParticipanteJuegosDto { Id = Guid.NewGuid() },
                        new PreguntaParticipanteJuegosDto { Id = Guid.NewGuid() }
                    }
                });

            Finalizacion.Setup(f => f.FinalizarSesionSiDuracionVencidaAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        public ServicioProgresoSecuencialSesion CrearServicio() => new(
            Usuario.Object,
            RepositorioSesiones.Object,
            ClienteTrivia.Object,
            RespuestasTrivia.Object,
            EvidenciasTesoro.Object,
            EtapasCompletadas.Object,
            Reloj.Object,
            new ServicioTiempoTriviaSesion(),
            Finalizacion.Object);
    }

    [Fact]
    public async Task ObtenerProgreso_UsaOrdenGlobalEntreMisiones()
    {
        var ctx = new Contexto();
        ctx.Sesion.AvanzarASiguienteEtapa(
            EtapaTesoro, MisionB, EtapaTrivia, TriviaId, "Trivia", 2, AhoraUtc, 60);
        ctx.EtapasCompletadas.Setup(r => r.ObtenerCompletadasAsync(
                ctx.Sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { EtapaTesoro });
        ctx.RespuestasTrivia.Setup(r => r.ContarPreguntasDistintasDeJugadorEnEtapaAsync(
                ctx.Sesion.Id, EtapaTrivia, ParticipanteId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var progreso = await ctx.CrearServicio().ObtenerParaParticipanteActualAsync(
            ctx.Sesion.Id, CancellationToken.None);

        progreso.EtapasCompletadasIds.Should().Equal(new[] { EtapaTesoro });
        progreso.MisionActualId.Should().Be(MisionB);
        progreso.EtapaActualId.Should().Be(EtapaTrivia);
        progreso.OrdenGlobalActual.Should().Be(2);
        progreso.TodoCompletado.Should().BeFalse();
    }

    [Fact]
    public async Task ValidarEtapaActual_RechazaEtapaFutura()
    {
        var ctx = new Contexto();
        ctx.EvidenciasTesoro.Setup(r => r.ExisteEvidenciaValidaIndividualAsync(
                ctx.Sesion.Id, EtapaTesoro, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> accion = () => ctx.CrearServicio().ValidarEtapaActualAsync(
            ctx.Sesion,
            ParticipanteId,
            MisionB,
            EtapaTrivia,
            "Trivia",
            TriviaId,
            CancellationToken.None);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
    }
}
