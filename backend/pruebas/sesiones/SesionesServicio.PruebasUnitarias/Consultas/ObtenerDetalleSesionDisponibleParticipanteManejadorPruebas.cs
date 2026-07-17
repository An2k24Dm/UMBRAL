using SesionesServicio.Aplicacion.Consultas.ObtenerDetalleSesionDisponibleParticipante;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Consultas;

public class ObtenerDetalleSesionDisponibleParticipanteManejadorPruebas
{
    private static readonly DateTime Ahora = new(2026, 7, 16, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_SesionInexistenteOCancelada_LanzaNoEncontrada()
    {
        var escenario = new Escenario();
        escenario.Repositorio.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        await escenario.Manejador.Invoking(m => m.Handle(
                new ObtenerDetalleSesionDisponibleParticipanteConsulta(Guid.NewGuid()),
                CancellationToken.None))
            .Should().ThrowAsync<SesionNoEncontradaExcepcion>();

        var cancelada = SesionIndividual.Rehidratar(
            Guid.NewGuid(), "Cancelada", "D", EstadoSesion.Cancelada,
            Ahora, "COD", Guid.NewGuid(), Ahora, null, null, 5);
        escenario.Repositorio.Setup(r => r.ObtenerPorIdAsync(cancelada.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelada);

        await escenario.Manejador.Invoking(m => m.Handle(
                new ObtenerDetalleSesionDisponibleParticipanteConsulta(cancelada.Id),
                CancellationToken.None))
            .Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task Handle_IndividualDisponible_MapeaParticipacionMisionEtapasYPuedeIngresar()
    {
        var participanteId = Guid.NewGuid();
        var misionId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var modoId = Guid.NewGuid();
        var sesion = SesionIndividual.Crear(
            "Individual", "Demo", Ahora.AddHours(1), "ABC123", Guid.NewGuid(), Ahora, 5);
        sesion.AsignarMisiones(new[] { misionId });
        sesion.Preparar();
        var participante = sesion.AgregarParticipante(participanteId, Ahora);
        sesion.EstablecerSecuenciaEtapas(new[]
        {
            EjecucionActualSesion.Planificar(misionId, etapaId, modoId, "Trivia", 1, 1, 1, 60)
        });
        sesion.IniciarPrimeraEtapa(sesion.SecuenciaEtapas[0], Ahora);
        var escenario = new Escenario(participanteId);
        escenario.Repositorio.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        escenario.ClienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(misionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MisionConEtapasJuegosDto
            {
                Id = misionId,
                Nombre = "Misión Uno",
                Descripcion = "Explorar",
                Dificultad = "Media",
                Etapas = new()
                {
                    new EtapaJuegosDto
                    {
                        Id = etapaId,
                        Orden = 2,
                        TipoModoDeJuego = "Trivia",
                        ModoDeJuegoId = modoId,
                        NombreModoDeJuego = "Trivia rápida",
                        TiempoEstimado = 30
                    }
                }
            });

        var detalle = await escenario.Manejador.Handle(
            new ObtenerDetalleSesionDisponibleParticipanteConsulta(sesion.Id),
            CancellationToken.None);

        detalle.Id.Should().Be(sesion.Id);
        detalle.ParticipacionActual.EstaInscrito.Should().BeTrue();
        detalle.ParticipacionActual.Tipo.Should().Be("Individual");
        detalle.ParticipacionActual.ParticipanteSesionId.Should().Be(participante.Id);
        detalle.PuedeIngresar.Should().BeTrue();
        detalle.EjecucionActual.Should().NotBeNull();
        detalle.Misiones.Should().ContainSingle();
        detalle.Misiones[0].Nombre.Should().Be("Misión Uno");
        detalle.Misiones[0].Etapas.Should().ContainSingle(e => e.Id == etapaId);
    }

    [Fact]
    public async Task Handle_GrupalParticipanteEnOtroEquipo_BloqueaIngresoYMapeaLiderazgo()
    {
        var participanteId = Guid.NewGuid();
        var sesion = SesionGrupal.Crear(
            "Grupal", "Demo", Ahora.AddHours(1), "XYZ789", Guid.NewGuid(), Ahora, 4, 3);
        sesion.Preparar();
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"),
            TipoEquipo.Publico,
            null,
            participanteId,
            Ahora,
            Ahora);
        var otraSesionId = Guid.NewGuid();
        var escenario = new Escenario(participanteId);
        escenario.Repositorio.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        escenario.Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                participanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SesionParticipacionActivaDto(
                otraSesionId, "Otra", EstadoSesion.EnPreparacion, ModoSesion.Grupal, null, null));

        var detalle = await escenario.Manejador.Handle(
            new ObtenerDetalleSesionDisponibleParticipanteConsulta(sesion.Id),
            CancellationToken.None);

        detalle.ParticipacionActual.EstaInscrito.Should().BeTrue();
        detalle.ParticipacionActual.Tipo.Should().Be("Equipo");
        detalle.ParticipacionActual.EquipoId.Should().Be(equipo.Id);
        detalle.ParticipacionActual.EquipoNombre.Should().Be("Rojo");
        detalle.ParticipacionActual.EsLider.Should().BeTrue();
        detalle.PuedeIngresar.Should().BeFalse();
        detalle.SesionActualId.Should().Be(otraSesionId);
        detalle.MotivoNoPuedeIngresar.Should().Contain("otra sesión");
    }

    private sealed class Escenario
    {
        public Mock<IRepositorioSesiones> Repositorio { get; } = new();
        public Mock<IClienteJuegosMisiones> ClienteMisiones { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IConsultasSesiones> Consultas { get; } = new();
        public Mock<IServicioFinalizacionSesion> Finalizacion { get; } = new();
        public ObtenerDetalleSesionDisponibleParticipanteManejador Manejador { get; }

        public Escenario(Guid? participanteId = null)
        {
            Usuario.Setup(u => u.ObtenerId()).Returns(participanteId);
            Finalizacion.Setup(f => f.FinalizarSesionSiDuracionVencidaAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            Manejador = new ObtenerDetalleSesionDisponibleParticipanteManejador(
                Repositorio.Object,
                ClienteMisiones.Object,
                Usuario.Object,
                Consultas.Object,
                Finalizacion.Object);
        }
    }
}
