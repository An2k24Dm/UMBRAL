using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.LiberarPista;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class LiberarPistaManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid PistaId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private const string Contenido = "Busca cerca del árbol más alto";

    private static SesionIndividual SesionActiva()
    {
        var s = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "PIST01", Operador, AhoraUtc, 5);
        s.Preparar();
        s.Iniciar(AhoraUtc);
        return s;
    }

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IRepositorioPistasLiberadas> RepoPistas { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Guid SesionId { get; }

        public Contexto(Sesion sesion, bool pistaYaLiberada = false)
        {
            SesionId = sesion.Id;

            Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            RepoPistas.Setup(r => r.ExistePistaLiberadaAsync(
                    sesion.Id, EtapaId, PistaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pistaYaLiberada);

            RepoPistas.Setup(r => r.AgregarAsync(
                    It.IsAny<PistaLiberadaRegistro>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Notificador.Setup(n => n.NotificarPistaLiberadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<double?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public LiberarPistaManejador Construir()
            => new(Repo.Object, RepoPistas.Object, Notificador.Object);

        public Task Ejecutar(Guid? pistaId = null, string? contenido = Contenido)
            => Construir().Handle(
                new LiberarPistaComando(SesionId, EtapaId, pistaId, contenido, "Texto", null, null),
                CancellationToken.None);
    }

    [Fact]
    public async Task ConPistaId_PersistirYNotificar()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion);

        await ctx.Ejecutar(pistaId: PistaId);

        ctx.RepoPistas.Verify(r => r.AgregarAsync(
            It.Is<PistaLiberadaRegistro>(x =>
                x.SesionId == sesion.Id &&
                x.EtapaId == EtapaId &&
                x.PistaId == PistaId &&
                x.Contenido == Contenido),
            It.IsAny<CancellationToken>()), Times.Once);

        ctx.Notificador.Verify(n => n.NotificarPistaLiberadaAsync(
            sesion.Id, EtapaId, PistaId, Contenido,
            It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<double?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SinPistaId_PistPersonalizada_PersistirYNotificarConPistaIdNulo()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion);
        const string contenidoPersonalizado = "Pista personalizada del operador";

        await ctx.Ejecutar(pistaId: null, contenido: contenidoPersonalizado);

        ctx.RepoPistas.Verify(r => r.AgregarAsync(
            It.Is<PistaLiberadaRegistro>(x =>
                x.PistaId == null &&
                x.Contenido == contenidoPersonalizado),
            It.IsAny<CancellationToken>()), Times.Once);

        ctx.Notificador.Verify(n => n.NotificarPistaLiberadaAsync(
            sesion.Id, EtapaId, null, contenidoPersonalizado,
            It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<double?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SesionNoEncontrada_LanzaInvalidOperationException()
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);
        var manejador = new LiberarPistaManejador(
            repo.Object, Mock.Of<IRepositorioPistasLiberadas>(),
            Mock.Of<INotificadorSesionesTiempoReal>());

        Func<Task> accion = () => manejador.Handle(
            new LiberarPistaComando(Guid.NewGuid(), EtapaId, PistaId, Contenido, "Texto", null, null),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task SesionNoActiva_LanzaInvalidOperationException()
    {
        var sesion = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "PIST01", Operador, AhoraUtc, 5);
        sesion.Preparar(); // EnPreparacion, no Activa
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(pistaId: PistaId);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sesiones activas*");
    }

    [Fact]
    public async Task PistaYaLiberada_LanzaInvalidOperationException()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, pistaYaLiberada: true);

        Func<Task> accion = () => ctx.Ejecutar(pistaId: PistaId);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue liberada*");
    }

    [Fact]
    public async Task ConPistaIdPeroSinContenido_LanzaInvalidOperationException()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(pistaId: PistaId, contenido: null);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contenido*");
    }

    [Fact]
    public async Task SinPistaIdNiContenido_LanzaInvalidOperationException()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(pistaId: null, contenido: "   ");

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contenido*");
    }

    [Fact]
    public async Task PistaLiberada_NoVerificaDuplicadoSiNoTienePistaId()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, pistaYaLiberada: true); // aunque ya exista, sin pistaId no se verifica
        const string contenidoPersonalizado = "Pista libre sin id";

        await ctx.Ejecutar(pistaId: null, contenido: contenidoPersonalizado);

        ctx.RepoPistas.Verify(r => r.ExistePistaLiberadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
