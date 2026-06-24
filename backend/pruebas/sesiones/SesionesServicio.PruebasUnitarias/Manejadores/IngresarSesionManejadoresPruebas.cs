using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionIndividual;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class IngresarSesionManejadoresPruebas
{
    private static readonly DateTime AhoraUtc =
        new(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ParticipanteId =
        Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid OperadorId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid SesionId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repositorio { get; } = new();
        public Mock<IUnidadTrabajoSesiones> UnidadTrabajo { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Mock<IConsultasSesiones> Consultas { get; } = new();
        public Mock<IClienteJuegosMisiones> Misiones { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();

        public Contexto(Sesion sesion)
        {
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => roles.Contains("Participante"));
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SesionParticipacionActivaDto?)null);
            Repositorio.Setup(r => r.ObtenerPorCodigoAsync(
                    "ABC123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repositorio.Setup(r => r.ObtenerPorIdAsync(
                    SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repositorio.Setup(r => r.ActualizarAsync(
                    It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnidadTrabajo.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarParticipantesSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private ConstructorRespuestaIngresoSesion ConstructorRespuesta()
            => new(Misiones.Object);

        public IngresarSesionPorCodigoManejador PorCodigo()
            => new(
                new ValidadorIngresarSesionPorCodigo(),
                Repositorio.Object,
                UnidadTrabajo.Object,
                Usuario.Object,
                Reloj.Object,
                new PoliticaParticipacionUnicaSesion(Consultas.Object),
                ConstructorRespuesta(),
                Notificador.Object);

        public IngresarSesionIndividualManejador Individual()
            => new(
                Repositorio.Object,
                UnidadTrabajo.Object,
                Usuario.Object,
                Reloj.Object,
                new PoliticaParticipacionUnicaSesion(Consultas.Object),
                ConstructorRespuesta(),
                Notificador.Object);
    }

    private static SesionIndividual IndividualEn(EstadoSesion estado)
        => SesionIndividual.Rehidratar(
            SesionId, "Individual", "Demo", estado,
            AhoraUtc.AddHours(1), "ABC123", OperadorId, AhoraUtc,
            null, null, 2);

    private static SesionGrupal GrupalEnPreparacion()
        => SesionGrupal.Rehidratar(
            SesionId, "Grupal", "Demo", EstadoSesion.EnPreparacion,
            AhoraUtc.AddHours(1), "ABC123", OperadorId, AhoraUtc,
            null, null, 3, 2);

    private static IngresarSesionPorCodigoComando Comando(string codigo = " abc123 ")
        => new(new IngresarSesionDto { CodigoSesion = codigo });

    [Fact]
    public async Task CodigoIndividualEnPreparacion_RegistraParticipante()
    {
        var sesion = IndividualEn(EstadoSesion.EnPreparacion);
        var contexto = new Contexto(sesion);

        var respuesta = await contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        respuesta.IngresoRegistrado.Should().BeTrue();
        respuesta.Modo.Should().Be("Individual");
        respuesta.ParticipacionActual!.EstaInscrito.Should().BeTrue();
        sesion.Participantes.Should().ContainSingle(
            p => p.ParticipanteIdentidadId == ParticipanteId && p.EquipoId == null);
        contexto.Repositorio.Verify(r => r.ActualizarAsync(
            sesion, It.IsAny<CancellationToken>()), Times.Once);
        contexto.UnidadTrabajo.Verify(u => u.GuardarCambiosAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        contexto.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            SesionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CodigoGrupal_NoRegistraParticipanteNiModificaAgregado()
    {
        var sesion = GrupalEnPreparacion();
        var contexto = new Contexto(sesion);
        var equiposAntes = sesion.Equipos.Count;

        var respuesta = await contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        respuesta.Modo.Should().Be("Grupal");
        respuesta.IngresoRegistrado.Should().BeFalse();
        respuesta.RedirigirADetalle.Should().BeTrue();
        respuesta.RequiereEquipo.Should().BeTrue();
        respuesta.ParticipacionActual!.EstaInscrito.Should().BeFalse();
        respuesta.Mensaje.Should().Be(
            "Esta sesión es grupal. Para ingresar debes crear un equipo o unirte a uno existente.");
        sesion.Equipos.Should().HaveCount(equiposAntes);
        sesion.Equipos.SelectMany(e => e.Participantes).Should().BeEmpty();
        contexto.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        contexto.UnidadTrabajo.Verify(u => u.GuardarCambiosAsync(
            It.IsAny<CancellationToken>()), Times.Never);
        contexto.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        contexto.Consultas.Verify(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SesionGrupal_NoExponeListaPropiaDeParticipantes()
    {
        typeof(SesionGrupal).GetProperty("Participantes").Should().BeNull();
        typeof(SesionGrupal).GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
            .Select(campo => campo.Name)
            .Should().NotContain("_participantes");
    }

    [Fact]
    public void ParticipanteGrupal_SoloExisteDentroDeEquipoYSiempreTieneEquipoId()
    {
        var sesion = GrupalEnPreparacion();

        var equipo = sesion.CrearEquipo(
            SesionesServicio.Dominio.ObjetosValor.NombreEquipo.Crear("Rojo"),
            TipoEquipo.Publico,
            null,
            ParticipanteId,
            AhoraUtc,
            AhoraUtc);

        equipo.Participantes.Should().ContainSingle();
        equipo.Participantes[0].EquipoId.Should().Be(equipo.Id);
        sesion.Equipos.SelectMany(e => e.Participantes)
            .Should().ContainSingle(p => p.ParticipanteIdentidadId == ParticipanteId);
    }

    [Fact]
    public async Task Respuesta_DevuelveContenidoOrdenado()
    {
        var misionA = Guid.NewGuid();
        var misionB = Guid.NewGuid();
        var misiones = new[]
        {
            SesionMision.Rehidratar(Guid.NewGuid(), SesionId, misionB, 2),
            SesionMision.Rehidratar(Guid.NewGuid(), SesionId, misionA, 1)
        };
        var sesion = SesionGrupal.Rehidratar(
            SesionId, "Grupal", "Demo", EstadoSesion.EnPreparacion,
            AhoraUtc.AddHours(1), "ABC123", OperadorId, AhoraUtc,
            null, null, 3, 2, misiones);
        var contexto = new Contexto(sesion);
        contexto.Misiones.Setup(c => c.ObtenerMisionConEtapasAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new MisionConEtapasJuegosDto
            {
                Id = id,
                Nombre = id == misionA ? "Primera" : "Segunda"
            });

        var respuesta = await contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        respuesta.Contenido.Select(c => c.MisionId).Should().Equal(misionA, misionB);
        respuesta.Contenido.Select(c => c.Orden).Should().Equal(1, 2);
    }

    [Fact]
    public async Task CodigoIndividual_Repetido_NoDuplica()
    {
        var existente = Participante.CrearParaSesionIndividual(
            SesionId, ParticipanteId, AhoraUtc.AddMinutes(-1));
        var sesion = SesionIndividual.Rehidratar(
            SesionId, "Individual", "Demo", EstadoSesion.EnPreparacion,
            AhoraUtc.AddHours(1), "ABC123", OperadorId, AhoraUtc,
            null, null, 2, participantes: new[] { existente });
        var contexto = new Contexto(sesion);

        var respuesta = await contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        respuesta.YaPertenecia.Should().BeTrue();
        sesion.Participantes.Should().ContainSingle();
        contexto.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        contexto.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CodigoIndividual_SiGuardadoFalla_NoNotificaTiempoReal()
    {
        var sesion = IndividualEn(EstadoSesion.EnPreparacion);
        var contexto = new Contexto(sesion);
        contexto.UnidadTrabajo.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo db"));

        Func<Task> accion = () => contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        contexto.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task CodigoIndividual_NoEnPreparacion_Bloquea(EstadoSesion estado)
    {
        var contexto = new Contexto(IndividualEn(estado));

        Func<Task> accion = () => contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>()
            .WithMessage("Solo puedes ingresar a una sesión en estado En Preparación.");
    }

    [Fact]
    public async Task Individual_BloqueaSiParticipaEnOtraSesion()
    {
        var contexto = new Contexto(IndividualEn(EstadoSesion.EnPreparacion));
        contexto.Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SesionParticipacionActivaDto(
                Guid.NewGuid(), "Otra", EstadoSesion.Activa,
                ModoSesion.Individual, null, null));

        Func<Task> accion = () => contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipanteYaEstaEnSesionActivaExcepcion>();
    }

    [Fact]
    public async Task EndpointIndividual_RechazaSesionGrupal()
    {
        var contexto = new Contexto(GrupalEnPreparacion());

        Func<Task> accion = () => contexto.Individual().Handle(
            new IngresarSesionIndividualComando(SesionId), CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>()
            .WithMessage("Para ingresar a una sesión grupal debes crear o unirte a un equipo.");
    }

    [Fact]
    public async Task CodigoInexistente_LanzaNoEncontrada()
    {
        var contexto = new Contexto(IndividualEn(EstadoSesion.EnPreparacion));
        contexto.Repositorio.Setup(r => r.ObtenerPorCodigoAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task UsuarioNoParticipante_SeRechaza()
    {
        var contexto = new Contexto(IndividualEn(EstadoSesion.EnPreparacion));
        contexto.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns(false);

        Func<Task> accion = () => contexto.PorCodigo().Handle(
            Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }
}
