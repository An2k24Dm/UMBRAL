using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionParticipante;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU52 — Orquestación de AplicarPenalizacionParticipanteManejador (sesión
// Individual). Cubre autorización, estado, objetivo, validación de puntos/motivo,
// registro + Outbox transaccional y contrato del evento publicado.
public sealed class AplicarPenalizacionParticipanteManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class EventoPublicado
    {
        public Guid EventoId;
        public Guid PenalizacionId;
        public Guid SesionId;
        public string TipoObjetivo = string.Empty;
        public Guid? ParticipanteSesionId;
        public Guid? ParticipanteIdentidadId;
        public Guid? EquipoId;
        public int Puntos;
        public string Motivo = string.Empty;
        public Guid OperadorIdentidadId;
    }

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IRepositorioPenalizacionesSesion> RepoPen { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IPublicadorEventosRanking> Publicador { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public PenalizacionSesion? Registrada;
        public EventoPublicado? Publicado;
        public int OrdenAgregar = -1;
        public int OrdenPublicar = -1;
        private int _contador;

        public Contexto(Sesion? sesion, Guid? usuarioId = null, string rol = "Operador")
        {
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);

            Repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);

            RepoPen.Setup(r => r.AgregarAsync(
                    It.IsAny<PenalizacionSesion>(), It.IsAny<CancellationToken>()))
                .Callback<PenalizacionSesion, CancellationToken>((p, _) =>
                {
                    Registrada = p;
                    OrdenAgregar = _contador++;
                })
                .Returns(Task.CompletedTask);

            Publicador.Setup(p => p.PublicarPenalizacionAplicadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Callback(new Action<Guid, Guid, Guid, string, Guid?, Guid?, Guid?, int, string, Guid, DateTime, CancellationToken>(
                    (evento, penal, ses, tipo, ps, pi, eq, pts, mot, op, _, __) =>
                    {
                        Publicado = new EventoPublicado
                        {
                            EventoId = evento, PenalizacionId = penal, SesionId = ses,
                            TipoObjetivo = tipo, ParticipanteSesionId = ps,
                            ParticipanteIdentidadId = pi, EquipoId = eq,
                            Puntos = pts, Motivo = mot, OperadorIdentidadId = op
                        };
                        OrdenPublicar = _contador++;
                    }))
                .Returns(Task.CompletedTask);

            Unidad.Setup(u => u.EjecutarEnTransaccionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));
        }

        public AplicarPenalizacionParticipanteManejador Construir()
            => new(
                Repo.Object, RepoPen.Object, Unidad.Object, Publicador.Object,
                Usuario.Object, Reloj.Object,
                new ValidadorAplicarPenalizacionParticipante(),
                Mock.Of<IRegistroLogsAplicacion>());

        public Task<Aplicacion.Comandos.Penalizaciones.PenalizacionEncoladaDto> Ejecutar(
            Guid sesionId, Guid participanteSesionId, int puntos = 5, string? motivo = "Incumplió una regla")
            => Construir().Handle(
                new AplicarPenalizacionParticipanteComando(sesionId, participanteSesionId, puntos, motivo),
                CancellationToken.None);
    }

    private static SesionIndividual CrearSesionIndividual(out Guid participanteSesionId,
        EstadoSesion estado = EstadoSesion.Activa)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        participanteSesionId = sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc).Id;
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Pausada) sesion.Pausar(AhoraUtc);
        if (estado == EstadoSesion.Finalizada) sesion.Finalizar(AhoraUtc);
        return sesion;
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task OperadorPropietario_penalizaParticipante_registraYPublica(EstadoSesion estado)
    {
        var sesion = CrearSesionIndividual(out var pid, estado);
        var ctx = new Contexto(sesion);

        var resultado = await ctx.Ejecutar(sesion.Id, pid);

        resultado.Estado.Should().Be("Pendiente");
        resultado.PenalizacionId.Should().NotBe(Guid.Empty);
        resultado.EventoId.Should().NotBe(Guid.Empty);
        ctx.Registrada.Should().NotBeNull();
        ctx.Registrada!.TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Participante);
        ctx.Registrada.Motivo.Should().Be("Incumplió una regla");
        ctx.Registrada.AplicadaEnUtc.Should().Be(AhoraUtc);
        ctx.Registrada.OperadorIdentidadId.Should().Be(Operador);
        ctx.Publicador.Verify(p => p.PublicarPenalizacionAplicadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Registro_yOutbox_ocurrenEnEseOrdenDentroDeTransaccion()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion);

        await ctx.Ejecutar(sesion.Id, pid);

        ctx.OrdenAgregar.Should().Be(0);
        ctx.OrdenPublicar.Should().Be(1);
        ctx.Unidad.Verify(u => u.EjecutarEnTransaccionAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublicadorRecibeContratoCorrecto()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var participante = sesion.Participantes.Single(p => p.Id == pid);
        var ctx = new Contexto(sesion);

        var resultado = await ctx.Ejecutar(sesion.Id, pid, puntos: 7, motivo: "Regla rota");

        ctx.Publicado.Should().NotBeNull();
        ctx.Publicado!.TipoObjetivo.Should().Be("Participante");
        ctx.Publicado.ParticipanteSesionId.Should().Be(pid);
        ctx.Publicado.ParticipanteIdentidadId.Should().Be(participante.ParticipanteIdentidadId);
        ctx.Publicado.EquipoId.Should().BeNull();
        ctx.Publicado.Puntos.Should().Be(7);
        ctx.Publicado.Motivo.Should().Be("Regla rota");
        ctx.Publicado.OperadorIdentidadId.Should().Be(Operador);
        ctx.Publicado.EventoId.Should().Be(resultado.EventoId);
        ctx.Publicado.PenalizacionId.Should().Be(resultado.PenalizacionId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task PuntosEnLimites_aceptados(int puntos)
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion);

        var resultado = await ctx.Ejecutar(sesion.Id, pid, puntos);

        resultado.Estado.Should().Be("Pendiente");
        ctx.Registrada!.Puntos.Should().Be(puntos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task PuntosFueraDeRango_rechazados400(int puntos)
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, pid, puntos);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
        ctx.Publicador.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MotivoInvalido_rechazado400(string? motivo)
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, pid, 5, motivo);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task MotivoMayorA500_rechazado400()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, pid, 5, new string('a', 501));

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Finalizada)]
    public async Task EstadoNoPermitido_rechazado409(EstadoSesion estado)
    {
        var sesion = CrearSesionIndividual(out var pid, estado);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, pid);

        await accion.Should().ThrowAsync<PenalizacionNoPermitidaExcepcion>();
        ctx.Publicador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OtroOperador_rechazado403()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion, usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, pid);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Participante")]
    public async Task RolNoOperador_rechazado403(string rol)
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion, rol: rol);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, pid);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_rechazada404()
    {
        var ctx = new Contexto(sesion: null);

        Func<Task> accion = () => ctx.Ejecutar(Guid.NewGuid(), Guid.NewGuid());

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task ObjetivoInexistente_rechazado404()
    {
        var sesion = CrearSesionIndividual(out _);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, Guid.NewGuid());

        await accion.Should().ThrowAsync<ParticipanteNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task ObjetivoDeOtraSesion_rechazado404()
    {
        var sesionObjetivo = CrearSesionIndividual(out var pidOtro);
        var sesionActual = CrearSesionIndividual(out _);
        var ctx = new Contexto(sesionActual);

        // Se usa el id de un participante que pertenece a otra sesión.
        Func<Task> accion = () => ctx.Ejecutar(sesionActual.Id, pidOtro);

        await accion.Should().ThrowAsync<ParticipanteNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task EndpointIndividualConSesionGrupal_rechazado400()
    {
        var grupal = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
        grupal.AsignarMisiones(new[] { Guid.NewGuid() });
        grupal.Preparar();
        var ctx = new Contexto(grupal);

        Func<Task> accion = () => ctx.Ejecutar(grupal.Id, Guid.NewGuid());

        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task CadaSolicitud_generaEventoIdUnico()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var ctx = new Contexto(sesion);

        var r1 = await ctx.Ejecutar(sesion.Id, pid);
        var r2 = await ctx.Ejecutar(sesion.Id, pid);

        r1.EventoId.Should().NotBe(r2.EventoId);
        r1.PenalizacionId.Should().NotBe(r2.PenalizacionId);
    }
}
