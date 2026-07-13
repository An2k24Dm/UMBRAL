using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Comandos.IngresarEquipo;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU47 — Orquestación de IngresarEquipoManejador: rol Participante,
// participación única, sesión grupal EnPreparacion, cupos, contraseña de
// equipos privados (verificada contra el hash) y notificación tras guardar.
public class IngresarEquipoManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Participante = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string HashGuardado = "hash-guardado";
    private const string ContrasenaCorrecta = "secreta123";

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IHashContrasenaEquipo> Hash { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Mock<IConsultasSesiones> Consultas { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IPublicadorEventosRanking> PublicadorRanking { get; } = new();
        public Sesion? Actualizada;
        public Guid SesionId;
        public Guid EquipoId;

        public Contexto(
            Sesion sesion, Guid equipoId,
            Guid? usuarioId = null, string rol = "Participante")
        {
            SesionId = sesion.Id;
            EquipoId = equipoId;

            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Participante);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);

            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SesionParticipacionActivaDto?)null);

            // Verificación de contraseña contra el hash, nunca en texto plano.
            Hash.Setup(h => h.Verificar(ContrasenaCorrecta, HashGuardado)).Returns(true);
            Hash.Setup(h => h.Verificar(
                    It.Is<string>(c => c != ContrasenaCorrecta), It.IsAny<string>()))
                .Returns(false);

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((x, _) => Actualizada = x)
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquiposSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public IngresarEquipoManejador Construir()
            => new(
                new IngresarEquipoValidador(),
                Repo.Object,
                Unidad.Object,
                Usuario.Object,
                Hash.Object,
                Reloj.Object,
                new PoliticaParticipacionUnicaSesion(Consultas.Object),
                Notificador.Object,
                Mock.Of<IRegistroLogsAplicacion>(),
                PublicadorRanking.Object);

        public Task<IngresarEquipoRespuestaDto> Ejecutar(string? contrasena = null)
            => Construir().Handle(
                new IngresarEquipoComando(
                    SesionId, EquipoId, new IngresarEquipoDto { Contrasena = contrasena }),
                CancellationToken.None);
    }

    private static SesionGrupal SesionConEquipo(
        out Guid equipoId,
        TipoEquipo tipo = TipoEquipo.Publico,
        EstadoSesion estado = EstadoSesion.EnPreparacion,
        int maximoPorEquipo = 3,
        bool llenarEquipo = false)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: maximoPorEquipo);
        var contrasenaHash = tipo == TipoEquipo.Privado
            ? ContrasenaEquipoHash.Crear(HashGuardado)
            : null;
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), tipo, contrasenaHash, Lider, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;

        if (llenarEquipo)
        {
            // Completa el cupo restante con otros participantes.
            while (!equipo.EstaLleno())
                sesion.AgregarParticipanteAEquipo(
                    equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        }

        if (estado == EstadoSesion.Programada) return sesion;
        sesion.Preparar();
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Cancelada) { sesion.Cancelar(); return sesion; }
        return sesion;
    }

    [Fact]
    public async Task EquipoPublico_Ingresa_GuardaYNotifica()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);

        var respuesta = await ctx.Ejecutar();

        respuesta.EquipoId.Should().Be(equipoId);
        respuesta.EsMiEquipo.Should().BeTrue();
        respuesta.CantidadParticipantes.Should().Be(2);
        ((SesionGrupal)ctx.Actualizada!).Equipos.Single().Participantes
            .Should().Contain(p => p.ParticipanteIdentidadId == Participante);
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            ctx.SesionId, EstadoSesion.EnPreparacion.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
        var participanteSesion = ((SesionGrupal)ctx.Actualizada!).Equipos.Single()
            .Participantes.Single(p => p.ParticipanteIdentidadId == Participante);
        ctx.PublicadorRanking.Verify(p => p.PublicarParticipanteUnidoSesionAsync(
            ctx.SesionId,
            participanteSesion.Id,
            Participante,
            equipoId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EquipoPublico_IgnoraContrasenaEnviada()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);

        var respuesta = await ctx.Ejecutar("cualquier-cosa");

        respuesta.EsMiEquipo.Should().BeTrue();
        ctx.Hash.Verify(h => h.Verificar(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EquipoPrivado_ContrasenaCorrecta_Ingresa()
    {
        var sesion = SesionConEquipo(out var equipoId, TipoEquipo.Privado);
        var ctx = new Contexto(sesion, equipoId);

        var respuesta = await ctx.Ejecutar(ContrasenaCorrecta);

        respuesta.EquipoId.Should().Be(equipoId);
        ctx.Hash.Verify(h => h.Verificar(ContrasenaCorrecta, HashGuardado), Times.Once);
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EquipoPrivado_SinContrasena_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId, TipoEquipo.Privado);
        var ctx = new Contexto(sesion, equipoId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EquipoPrivado_ContrasenaIncorrecta_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId, TipoEquipo.Privado);
        var ctx = new Contexto(sesion, equipoId);

        Func<Task> accion = () => ctx.Ejecutar("incorrecta");

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>()
            .WithMessage("La contraseña del equipo es incorrecta.");
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task RolNoParticipante_Rechaza(string rol)
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId, rol: rol);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task NoAutenticado_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        ctx.Usuario.Setup(u => u.EstaAutenticado()).Returns(false);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task SesionIndividual_Rechaza()
    {
        var individual = SesionIndividual.Crear(
            "Ind", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        individual.Preparar();
        var ctx = new Contexto(SesionConEquipo(out var equipoId), equipoId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(individual);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task SesionNoEnPreparacion_Rechaza(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, estado: estado);
        var ctx = new Contexto(sesion, equipoId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out _);
        var ctx = new Contexto(sesion, Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task EquipoLleno_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId, maximoPorEquipo: 2, llenarEquipo: true);
        var ctx = new Contexto(sesion, equipoId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>()
            .WithMessage("El equipo no tiene cupos disponibles.");
    }

    [Fact]
    public async Task YaPerteneceAEstaSesion_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        // La política detecta participación activa en esta misma sesión.
        ctx.Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                Participante, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SesionParticipacionActivaDto(
                ctx.SesionId, "Sesión", EstadoSesion.EnPreparacion,
                ModoSesion.Grupal, equipoId, "Rojo"));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipanteYaPerteneceASesionExcepcion>();
    }

    [Fact]
    public async Task EnOtraSesionActiva_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        ctx.Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                Participante, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SesionParticipacionActivaDto(
                Guid.NewGuid(), "Otra", EstadoSesion.Activa,
                ModoSesion.Individual, null, null));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipanteYaEstaEnSesionActivaExcepcion>();
    }

    [Fact]
    public async Task SiGuardarFalla_NoNotifica()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        ctx.Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo db"));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<InvalidOperationException>();
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
