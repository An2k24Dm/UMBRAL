using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class EnviarEvidenciaTesoroManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid BusquedaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string CodigoValido = "QR-TESORO-001";

    private static SesionIndividual IndividualActiva()
    {
        var s = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc, 5);
        s.Preparar();
        s.AgregarParticipante(ParticipanteId, AhoraUtc);
        s.Iniciar(AhoraUtc);
        return s;
    }

    private static EnviarEvidenciaTesoroManejador Construir(
        Sesion sesion,
        Guid? participanteIdentidadId = null,
        bool yaEnvio = false,
        bool? esValida = true,
        int puntajeBase = 50,
        int conEvidenciaValida = 0)
    {
        var pid = participanteIdentidadId ?? ParticipanteId;

        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(pid);

        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        var repoEvidencias = new Mock<IRepositorioEvidenciasTesoro>();
        repoEvidencias.Setup(r => r.ExisteEvidenciaAsync(
                sesion.Id, EtapaId, pid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(yaEnvio);
        repoEvidencias.Setup(r => r.AgregarAsync(
                It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoEvidencias.Setup(r => r.ContarParticipantesConEvidenciaValidaAsync(
                sesion.Id, EtapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conEvidenciaValida);

        var clienteTesoro = new Mock<IClienteBusquedaTesoro>();
        clienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esValida);
        clienteTesoro.Setup(c => c.ObtenerBusquedaParticipanteAsync(
                BusquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusquedaTesoroJuegosDto { Puntaje = puntajeBase });

        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, clienteTesoro.Object,
            repoEvidencias.Object, notificador.Object,
            Mock.Of<IServicioFinalizacionSesion>());
    }

    private static EnviarEvidenciaTesoroComando Comando(Sesion sesion)
        => new(sesion.Id, MisionId, EtapaId, BusquedaId, CodigoValido);

    [Fact]
    public async Task CodigoValido_DevuelveEsValidaTrue_YPuntajeBase()
    {
        var sesion = IndividualActiva();
        var manejador = Construir(sesion, puntajeBase: 75);

        var resultado = await manejador.Handle(Comando(sesion), CancellationToken.None);

        resultado.EsValida.Should().BeTrue();
        resultado.PuntosGanados.Should().Be(75);
    }

    [Fact]
    public async Task CodigoInvalido_DevuelveEsValidaFalse_YCeroPuntos()
    {
        var sesion = IndividualActiva();
        var manejador = Construir(sesion, esValida: false);

        var resultado = await manejador.Handle(Comando(sesion), CancellationToken.None);

        resultado.EsValida.Should().BeFalse();
        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task CodigoInvalido_NoDisparaEtapaCompletada()
    {
        var sesion = IndividualActiva();
        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var repoEvidencias = new Mock<IRepositorioEvidenciasTesoro>();
        repoEvidencias.Setup(r => r.ExisteEvidenciaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoEvidencias.Setup(r => r.AgregarAsync(
                It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clienteTesoro = new Mock<IClienteBusquedaTesoro>();
        clienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, clienteTesoro.Object,
            repoEvidencias.Object, notificador.Object,
            Mock.Of<IServicioFinalizacionSesion>());

        var resultado = await manejador.Handle(Comando(sesion), CancellationToken.None);

        resultado.EtapaCompletada.Should().BeFalse();
        notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EtapaCompletada_CuandoTodosValidaron_NotificaYFinaliza()
    {
        var sesion = IndividualActiva(); // 1 participante
        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var servicioFinalizacion = new Mock<IServicioFinalizacionSesion>();
        servicioFinalizacion.Setup(s => s.FinalizarSiTodasEtapasCompletadasAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var repoEvidencias = new Mock<IRepositorioEvidenciasTesoro>();
        repoEvidencias.Setup(r => r.ExisteEvidenciaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoEvidencias.Setup(r => r.AgregarAsync(
                It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoEvidencias.Setup(r => r.ContarParticipantesConEvidenciaValidaAsync(
                sesion.Id, EtapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // >= totalJugadores(1)
        var clienteTesoro = new Mock<IClienteBusquedaTesoro>();
        clienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        clienteTesoro.Setup(c => c.ObtenerBusquedaParticipanteAsync(
                BusquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusquedaTesoroJuegosDto { Puntaje = 50 });

        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, clienteTesoro.Object,
            repoEvidencias.Object, notificador.Object, servicioFinalizacion.Object);

        var resultado = await manejador.Handle(Comando(sesion), CancellationToken.None);

        resultado.EtapaCompletada.Should().BeTrue();
        notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, MisionId, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        servicioFinalizacion.Verify(s => s.FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EtapaNoCompletada_CuandoFaltanJugadores_NoNotifica()
    {
        var sesion = IndividualActiva();
        // conEvidenciaValida=0 < totalJugadores=1
        var manejador = Construir(sesion, esValida: true, conEvidenciaValida: 0);

        var resultado = await manejador.Handle(Comando(sesion), CancellationToken.None);

        resultado.EtapaCompletada.Should().BeFalse();
    }

    [Fact]
    public async Task PersisteLaEvidencia()
    {
        var sesion = IndividualActiva();
        var repoEvidencias = new Mock<IRepositorioEvidenciasTesoro>();
        repoEvidencias.Setup(r => r.ExisteEvidenciaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoEvidencias.Setup(r => r.AgregarAsync(
                It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoEvidencias.Setup(r => r.ContarParticipantesConEvidenciaValidaAsync(
                sesion.Id, EtapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var clienteTesoro = new Mock<IClienteBusquedaTesoro>();
        clienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        clienteTesoro.Setup(c => c.ObtenerBusquedaParticipanteAsync(
                BusquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusquedaTesoroJuegosDto { Puntaje = 30 });

        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, clienteTesoro.Object,
            repoEvidencias.Object, Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        await manejador.Handle(Comando(sesion), CancellationToken.None);

        repoEvidencias.Verify(r => r.AgregarAsync(
            It.Is<EvidenciaTesoroRegistro>(x =>
                x.SesionId == sesion.Id &&
                x.ParticipanteIdentidadId == ParticipanteId &&
                x.EsValida == true &&
                x.PuntosGanados == 30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UsuarioNoAutenticado_LanzaUnauthorizedAccessException()
    {
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns((Guid?)null);
        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, Mock.Of<IRepositorioSesiones>(),
            Mock.Of<IClienteBusquedaTesoro>(), Mock.Of<IRepositorioEvidenciasTesoro>(),
            Mock.Of<INotificadorSesionesTiempoReal>(), Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarEvidenciaTesoroComando(Guid.NewGuid(), MisionId, EtapaId, BusquedaId, CodigoValido),
            CancellationToken.None);

        await accion.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SesionNoEncontrada_LanzaInvalidOperationException()
    {
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);
        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, Mock.Of<IClienteBusquedaTesoro>(),
            Mock.Of<IRepositorioEvidenciasTesoro>(), Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarEvidenciaTesoroComando(Guid.NewGuid(), MisionId, EtapaId, BusquedaId, CodigoValido),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task SesionNoActiva_LanzaInvalidOperationException()
    {
        var sesion = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc, 5);
        sesion.Preparar(); // EnPreparacion, no Activa
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, Mock.Of<IClienteBusquedaTesoro>(),
            Mock.Of<IRepositorioEvidenciasTesoro>(), Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarEvidenciaTesoroComando(sesion.Id, MisionId, EtapaId, BusquedaId, CodigoValido),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está activa*");
    }

    [Fact]
    public async Task ParticipanteNoInscrito_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var otroUsuario = Guid.NewGuid();
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(otroUsuario);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, Mock.Of<IClienteBusquedaTesoro>(),
            Mock.Of<IRepositorioEvidenciasTesoro>(), Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarEvidenciaTesoroComando(sesion.Id, MisionId, EtapaId, BusquedaId, CodigoValido),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está inscrito*");
    }

    [Fact]
    public async Task YaEnvioEvidencia_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var manejador = Construir(sesion, yaEnvio: true);

        Func<Task> accion = () => manejador.Handle(Comando(sesion), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya enviaste*");
    }

    [Fact]
    public async Task BusquedaNoEncontrada_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var repoEvidencias = new Mock<IRepositorioEvidenciasTesoro>();
        repoEvidencias.Setup(r => r.ExisteEvidenciaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var clienteTesoro = new Mock<IClienteBusquedaTesoro>();
        clienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null); // búsqueda no encontrada
        var manejador = new EnviarEvidenciaTesoroManejador(
            usuario.Object, repo.Object, clienteTesoro.Object,
            repoEvidencias.Object, Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(Comando(sesion), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no encontrada*");
    }
}
