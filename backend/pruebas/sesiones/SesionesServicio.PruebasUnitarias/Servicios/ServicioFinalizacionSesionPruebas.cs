using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Servicios;

public class ServicioFinalizacionSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static SesionIndividual SesionActiva()
    {
        var s = SesionIndividual.Crear(
            "Finalizacion", "Demo", AhoraUtc.AddHours(1), "FIN001", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new List<Guid> { MisionId });
        s.Preparar();
        s.Iniciar(AhoraUtc);
        return s;
    }

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> RepoSesiones { get; } = new();
        public Mock<IRepositorioEtapasCompletadas> RepoEtapas { get; } = new();
        public Mock<IClienteJuegosMisiones> ClienteMisiones { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IUnidadTrabajoSesiones> UnidadTrabajo { get; } = new();
        public Sesion? SesionActualizada;

        public Contexto(
            Sesion? sesion,
            int totalEtapasPorMision = 1,
            int etapasCompletadas = 1)
        {
            RepoSesiones.Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            RepoSesiones.Setup(r => r.ActualizarAsync(
                    It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((s, _) => SesionActualizada = s)
                .Returns(Task.CompletedTask);

            RepoEtapas.Setup(r => r.RegistrarAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            RepoEtapas.Setup(r => r.ContarAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(etapasCompletadas);

            ClienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(
                    MisionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionConEtapasJuegosDto
                {
                    Id = MisionId,
                    Etapas = BuildEtapas(totalEtapasPorMision)
                });

            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            UnidadTrabajo.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public ServicioFinalizacionSesion Construir()
            => new(
                RepoSesiones.Object,
                RepoEtapas.Object,
                ClienteMisiones.Object,
                Notificador.Object,
                UnidadTrabajo.Object,
                BuildReloj());

        public Task Ejecutar(Guid? sesionId = null)
            => Construir().FinalizarSiTodasEtapasCompletadasAsync(
                sesionId ?? SesionId(), EtapaId, CancellationToken.None);

        private Guid SesionId() => Guid.NewGuid();

        private static IProveedorFechaHora BuildReloj()
        {
            var r = new Mock<IProveedorFechaHora>();
            r.Setup(x => x.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            return r.Object;
        }

        private static List<EtapaJuegosDto> BuildEtapas(int count)
        {
            var list = new List<EtapaJuegosDto>();
            for (var i = 0; i < count; i++)
                list.Add(new EtapaJuegosDto { Id = Guid.NewGuid(), Orden = i + 1 });
            return list;
        }
    }

    [Fact]
    public async Task SesionNoExiste_NoFinaliza_NoLanzaExcepcion()
    {
        var ctx = new Contexto(sesion: null);
        var sesionId = Guid.NewGuid();

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesionId, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SesionNoActiva_NoFinaliza()
    {
        var sesion = SesionIndividual.Crear(
            "Finalizacion", "Demo", AhoraUtc.AddHours(1), "FIN001", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new List<Guid> { MisionId });
        sesion.Preparar(); // EnPreparacion, no Activa
        var ctx = new Contexto(sesion);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TodasEtapasCompletadas_FinalizaSesion()
    {
        var sesion = SesionActiva();
        // 1 mision con 1 etapa, 1 etapa completada → debe finalizar
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Finalizada);
        ctx.SesionActualizada.FechaFinalizacionUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public async Task TodasEtapasCompletadas_GuardaCambiosYNotifica()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.UnidadTrabajo.Verify(u => u.GuardarCambiosAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            sesion.Id, EstadoSesion.Finalizada.ToString(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoTodasEtapasCompletadas_NoFinaliza()
    {
        var sesion = SesionActiva();
        // 1 misión con 2 etapas, solo 1 completada → no debe finalizar
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SinEtapasEnMision_NoFinaliza()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 0, etapasCompletadas: 0);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SiempreRegistraEtapaCompletada_InclusivoAntesDeVerificar()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoEtapas.Verify(r => r.RegistrarAsync(
            sesion.Id, EtapaId, AhoraUtc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MisionRetornaNula_TrataCeroEtapas_NoFinaliza()
    {
        var sesion = SesionActiva();
        var repoSesiones = new Mock<IRepositorioSesiones>();
        repoSesiones.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var repoEtapas = new Mock<IRepositorioEtapasCompletadas>();
        repoEtapas.Setup(r => r.RegistrarAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoEtapas.Setup(r => r.ContarAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var clienteMisiones = new Mock<IClienteJuegosMisiones>();
        clienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(
                MisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MisionConEtapasJuegosDto?)null); // misión no encontrada
        var reloj = new Mock<IProveedorFechaHora>();
        reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
        var servicio = new ServicioFinalizacionSesion(
            repoSesiones.Object, repoEtapas.Object, clienteMisiones.Object,
            Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IUnidadTrabajoSesiones>(), reloj.Object);

        await servicio.FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        repoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
