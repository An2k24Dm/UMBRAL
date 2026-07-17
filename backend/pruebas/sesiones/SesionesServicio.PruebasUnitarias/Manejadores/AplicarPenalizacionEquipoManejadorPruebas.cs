using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionEquipo;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU52 — Orquestación de AplicarPenalizacionEquipoManejador (sesión Grupal).
public sealed class AplicarPenalizacionEquipoManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IRepositorioPenalizacionesSesion> RepoPen { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IPublicadorEventosRanking> Publicador { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public PenalizacionSesion? Registrada;
        public string? TipoPublicado;
        public Guid? EquipoPublicado;
        public Guid? ParticipantePublicado;

        public Contexto(Sesion? sesion, Guid? usuarioId = null, string rol = "Operador")
        {
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);
            Repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            RepoPen.Setup(r => r.AgregarAsync(It.IsAny<PenalizacionSesion>(), It.IsAny<CancellationToken>()))
                .Callback<PenalizacionSesion, CancellationToken>((p, _) => Registrada = p)
                .Returns(Task.CompletedTask);
            Publicador.Setup(p => p.PublicarPenalizacionAplicadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Callback(new Action<Guid, Guid, Guid, string, Guid?, Guid?, Guid?, int, string, Guid, DateTime, CancellationToken>(
                    (_, __, ___, tipo, ps, ____, eq, _____, ______, _______, ________, _________) =>
                    {
                        TipoPublicado = tipo;
                        EquipoPublicado = eq;
                        ParticipantePublicado = ps;
                    }))
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.EjecutarEnTransaccionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));
        }

        public AplicarPenalizacionEquipoManejador Construir()
            => new(
                Repo.Object, RepoPen.Object, Unidad.Object, Publicador.Object,
                Usuario.Object, Reloj.Object,
                new ValidadorAplicarPenalizacionEquipo(),
                Mock.Of<IRegistroLogsAplicacion>());

        public Task<Aplicacion.Comandos.Penalizaciones.PenalizacionEncoladaDto> Ejecutar(
            Guid sesionId, Guid equipoId, int puntos = 10, string? motivo = "Regla del equipo")
            => Construir().Handle(
                new AplicarPenalizacionEquipoComando(sesionId, equipoId, puntos, motivo),
                CancellationToken.None);
    }

    private static SesionGrupal CrearSesionGrupal(out Guid equipoId,
        EstadoSesion estado = EstadoSesion.Activa)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        equipoId = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc).Id;
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Pausada) sesion.Pausar(AhoraUtc);
        if (estado == EstadoSesion.Finalizada) sesion.Finalizar(AhoraUtc);
        return sesion;
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task OperadorPropietario_penalizaEquipo_registraYPublica(EstadoSesion estado)
    {
        var sesion = CrearSesionGrupal(out var equipoId, estado);
        var ctx = new Contexto(sesion);

        var resultado = await ctx.Ejecutar(sesion.Id, equipoId);

        resultado.Estado.Should().Be("Pendiente");
        ctx.Registrada!.TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Equipo);
        ctx.Registrada.EquipoId.Should().Be(equipoId);
        ctx.Registrada.ParticipanteSesionId.Should().BeNull();
        ctx.TipoPublicado.Should().Be("Equipo");
        ctx.EquipoPublicado.Should().Be(equipoId);
        ctx.ParticipantePublicado.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task PuntosEnLimites_aceptados(int puntos)
    {
        var sesion = CrearSesionGrupal(out var equipoId);
        var ctx = new Contexto(sesion);

        var resultado = await ctx.Ejecutar(sesion.Id, equipoId, puntos);

        resultado.Estado.Should().Be("Pendiente");
        ctx.Registrada!.Puntos.Should().Be(puntos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task PuntosFueraDeRango_rechazados400(int puntos)
    {
        var sesion = CrearSesionGrupal(out var equipoId);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, equipoId, puntos);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task EndpointGrupalConSesionIndividual_rechazado400()
    {
        var individual = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        individual.AsignarMisiones(new[] { Guid.NewGuid() });
        individual.Preparar();
        var ctx = new Contexto(individual);

        Func<Task> accion = () => ctx.Ejecutar(individual.Id, Guid.NewGuid());

        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task EquipoInexistente_rechazado404()
    {
        var sesion = CrearSesionGrupal(out _);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, Guid.NewGuid());

        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Finalizada)]
    public async Task EstadoNoPermitido_rechazado409(EstadoSesion estado)
    {
        var sesion = CrearSesionGrupal(out var equipoId, estado);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, equipoId);

        await accion.Should().ThrowAsync<PenalizacionNoPermitidaExcepcion>();
    }

    [Fact]
    public async Task OtroOperador_rechazado403()
    {
        var sesion = CrearSesionGrupal(out var equipoId);
        var ctx = new Contexto(sesion, usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, equipoId);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Participante")]
    public async Task RolNoOperador_rechazado403(string rol)
    {
        var sesion = CrearSesionGrupal(out var equipoId);
        var ctx = new Contexto(sesion, rol: rol);

        Func<Task> accion = () => ctx.Ejecutar(sesion.Id, equipoId);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }
}
