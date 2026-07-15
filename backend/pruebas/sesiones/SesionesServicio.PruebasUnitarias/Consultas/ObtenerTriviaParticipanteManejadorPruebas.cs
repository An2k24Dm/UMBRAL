using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Consultas;

// Demuestra la autoridad de lectura del contenido de Trivia (#2): el GET pasa
// por ValidarEtapaActualAsync antes de entregar el contenido. No se expone una
// Trivia de una etapa futura aunque el cliente conozca el TriviaId.
public class ObtenerTriviaParticipanteManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Participante = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MisionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EtapaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TriviaId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private sealed class Arranque
    {
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IRepositorioSesiones> Sesiones { get; } = new();
        public Mock<IClienteJuegosTrivia> ClienteTrivia { get; } = new();
        public Mock<IServicioProgresoSecuencialSesion> Progreso { get; } = new();

        public Arranque(Sesion sesion)
        {
            Usuario.Setup(u => u.ObtenerId()).Returns(Participante);
            Sesiones.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Progreso.Setup(p => p.ValidarEtapaActualAsync(
                    It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            ClienteTrivia.Setup(c => c.ObtenerTriviaParticipanteAsync(
                    TriviaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TriviaParticipanteJuegosDto { Id = TriviaId });
        }

        public ObtenerTriviaParticipanteManejador Construir()
            => new(Usuario.Object, Sesiones.Object, ClienteTrivia.Object, Progreso.Object);

        public Task<TriviaParticipanteJuegosDto?> EjecutarAsync(Guid sesionId)
            => Construir().Handle(
                new ObtenerTriviaParticipanteConsulta(sesionId, MisionId, EtapaId, TriviaId),
                CancellationToken.None);
    }

    private static SesionIndividual Individual()
    {
        var s = SesionIndividual.Crear("Trivia", "Demo", AhoraUtc.AddHours(1), "COD001", Operador, AhoraUtc, 5);
        s.Preparar();
        s.AgregarParticipante(Participante, AhoraUtc);
        s.Iniciar(AhoraUtc);
        return s;
    }

    [Fact]
    public async Task EtapaActual_DevuelveContenido()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.Should().NotBeNull();
        arr.ClienteTrivia.Verify(c => c.ObtenerTriviaParticipanteAsync(
            TriviaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (#2) Trivia de etapa futura: la lectura se bloquea y no se consulta contenido.
    public async Task EtapaFutura_BloqueaLecturaDeContenido()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);
        arr.Progreso.Setup(p => p.ValidarEtapaActualAsync(
                It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperacionSesionInvalidaExcepcion(
                "Debes completar la etapa anterior antes de jugar esta etapa."));

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
        arr.ClienteTrivia.Verify(c => c.ObtenerTriviaParticipanteAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SesionInexistente_LanzaSesionNoEncontrada()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);
        arr.Sesiones.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => arr.EjecutarAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task NoAutenticado_LanzaUnauthorized()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);
        arr.Usuario.Setup(u => u.ObtenerId()).Returns((Guid?)null);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
