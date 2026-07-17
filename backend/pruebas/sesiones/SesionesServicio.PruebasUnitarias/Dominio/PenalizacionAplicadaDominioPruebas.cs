using System.Reflection;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public sealed class PenalizacionAplicadaDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void CantidadPenalizacion_aceptaLimites(int valor)
        => CantidadPenalizacion.Crear(valor).Valor.Should().Be(valor);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CantidadPenalizacion_rechazaFueraDeRango(int valor)
        => FluentActions.Invoking(() => CantidadPenalizacion.Crear(valor))
            .Should().Throw<PenalizacionInvalidaExcepcion>();

    [Fact]
    public void PuntajeSesion_desdePersistencia_admiteNegativo()
        => PuntajeSesion.DesdePersistencia(-5).Valor.Should().Be(-5);

    [Fact]
    public void PuntajeSesion_ganado_sigueSinNegativos()
    {
        FluentActions.Invoking(() => PuntajeSesion.Crear(-1))
            .Should().Throw<ParticipacionInvalidaExcepcion>();
        FluentActions.Invoking(() => PuntajeSesion.Cero().Sumar(-1))
            .Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void CrearParaParticipante_registraHechoCompletoEInmutable()
    {
        var eventoId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        var penalizacion = PenalizacionAplicada.CrearParaParticipante(
            eventoId, sesionId, participanteSesionId, identidadId,
            5, "  Incumplió una regla  ", Operador, AhoraUtc);

        penalizacion.EventoId.Should().Be(eventoId);
        penalizacion.SesionId.Should().Be(sesionId);
        penalizacion.TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Participante);
        penalizacion.ParticipanteSesionId.Should().Be(participanteSesionId);
        penalizacion.ParticipanteIdentidadId.Should().Be(identidadId);
        penalizacion.EquipoId.Should().BeNull();
        penalizacion.PuntosDescontados.Should().Be(5);
        penalizacion.Motivo.Should().Be("Incumplió una regla");
        penalizacion.OperadorIdentidadId.Should().Be(Operador);
        penalizacion.AplicadaEnUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public void CrearParaEquipo_dejaParticipanteEnNull()
    {
        var equipoId = Guid.NewGuid();

        var penalizacion = PenalizacionAplicada.CrearParaEquipo(
            Guid.NewGuid(), Guid.NewGuid(), equipoId, 10, "Motivo", Operador, AhoraUtc);

        penalizacion.TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Equipo);
        penalizacion.EquipoId.Should().Be(equipoId);
        penalizacion.ParticipanteSesionId.Should().BeNull();
        penalizacion.ParticipanteIdentidadId.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void CrearParaParticipante_aceptaLimitesDePuntos(int puntos)
        => PenalizacionAplicada.CrearParaParticipante(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                puntos, "Motivo", Operador, AhoraUtc)
            .PuntosDescontados.Should().Be(puntos);

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(101)]
    public void CrearParaParticipante_rechazaPuntosFueraDeRango(int puntos)
        => FluentActions.Invoking(() => PenalizacionAplicada.CrearParaParticipante(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                puntos, "Motivo", Operador, AhoraUtc))
            .Should().Throw<PenalizacionInvalidaExcepcion>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearParaParticipante_rechazaMotivoInvalido(string? motivo)
        => FluentActions.Invoking(() => PenalizacionAplicada.CrearParaParticipante(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                5, motivo!, Operador, AhoraUtc))
            .Should().Throw<PenalizacionInvalidaExcepcion>();

    [Fact]
    public void CrearParaParticipante_rechazaMotivoMayorA500()
        => FluentActions.Invoking(() => PenalizacionAplicada.CrearParaParticipante(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                5, new string('a', 501), Operador, AhoraUtc))
            .Should().Throw<PenalizacionInvalidaExcepcion>();

    [Fact]
    public void CrearParaParticipante_aceptaMotivoDe500()
        => PenalizacionAplicada.CrearParaParticipante(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                5, new string('a', 500), Operador, AhoraUtc)
            .Motivo.Length.Should().Be(500);

    [Theory]
    [InlineData("MarcarProcesada")]
    [InlineData("MarcarFallida")]
    public void Evento_noExponeOperacionesDeActualizacion(string metodo)
        => typeof(PenalizacionAplicada).GetMethod(
            metodo,
            BindingFlags.Public | BindingFlags.Instance)
        .Should().BeNull();

    [Theory]
    [InlineData("EstadoProcesamiento")]
    [InlineData("ProcesadaEnUtc")]
    [InlineData("PuntajeResultante")]
    public void Evento_noExponePropiedadesTecnicasMutables(string propiedad)
        => typeof(PenalizacionAplicada).GetProperty(propiedad).Should().BeNull();

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public void ValidarPuedePenalizar_permiteActivaYPausada(EstadoSesion estado)
        => FluentActions.Invoking(() => SesionIndividualEnEstado(estado, out _).ValidarPuedePenalizar())
            .Should().NotThrow();

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void ValidarPuedePenalizar_rechazaOtrosEstados(EstadoSesion estado)
        => FluentActions.Invoking(() => SesionIndividualEnEstado(estado, out _).ValidarPuedePenalizar())
            .Should().Throw<PenalizacionNoPermitidaExcepcion>();

    [Fact]
    public void ObtenerParticipanteParaPenalizar_objetivoInexistente_lanza404()
        => FluentActions.Invoking(() => SesionIndividualEnEstado(EstadoSesion.Activa, out _)
                .ObtenerParticipanteParaPenalizar(Guid.NewGuid()))
            .Should().Throw<ParticipanteNoEncontradoExcepcion>();

    [Fact]
    public void ObtenerEquipoParaPenalizar_devuelveEquipoEnEstadoValido()
    {
        var sesion = SesionGrupalEnEstado(EstadoSesion.Pausada, out var equipoId);
        sesion.ObtenerEquipoParaPenalizar(equipoId).Id.Should().Be(equipoId);
    }

    [Fact]
    public void ObtenerEquipoParaPenalizar_equipoInexistente_lanza404()
        => FluentActions.Invoking(() => SesionGrupalEnEstado(EstadoSesion.Activa, out _)
                .ObtenerEquipoParaPenalizar(Guid.NewGuid()))
            .Should().Throw<EquipoNoEncontradoExcepcion>();

    [Fact]
    public void Participante_establecerPenalizacionSnapshot_fijaNegativoYMagnitud()
    {
        var sesion = SesionIndividualEnEstado(EstadoSesion.Activa, out var participanteSesionId);
        var participante = sesion.Participantes.Single(p => p.Id == participanteSesionId);

        participante.EstablecerPenalizacionSnapshot(13, -3, AhoraUtc).Should().BeTrue();

        participante.PuntosPenalizados.Should().Be(13);
        participante.Puntaje.Valor.Should().Be(-3);
        participante.SnapshotRankingUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public void Participante_snapshotAntiguo_noSobreescribe()
    {
        var sesion = SesionIndividualEnEstado(EstadoSesion.Activa, out var participanteSesionId);
        var participante = sesion.Participantes.Single(p => p.Id == participanteSesionId);
        participante.EstablecerPenalizacionSnapshot(10, -1, AhoraUtc);

        participante.EstablecerPenalizacionSnapshot(99, 50, AhoraUtc.AddSeconds(-5))
            .Should().BeFalse();

        participante.PuntosPenalizados.Should().Be(10);
        participante.Puntaje.Valor.Should().Be(-1);
    }

    [Fact]
    public void Equipo_establecerPenalizacionSnapshot_fijaEquipoSinTocarParticipantes()
    {
        var sesion = SesionGrupalEnEstado(EstadoSesion.Activa, out var equipoId);
        var equipo = sesion.Equipos.Single(e => e.Id == equipoId);
        var puntajeParticipanteAntes = equipo.Participantes[0].Puntaje.Valor;

        equipo.EstablecerPenalizacionSnapshot(20, -5, AhoraUtc);

        equipo.PuntosPenalizados.Should().Be(20);
        equipo.Puntaje.Valor.Should().Be(-5);
        equipo.Participantes[0].Puntaje.Valor.Should().Be(puntajeParticipanteAntes);
    }

    private static SesionIndividual SesionIndividualEnEstado(
        EstadoSesion estado, out Guid participanteSesionId)
    {
        var sesion = SesionIndividual.Crear(
            "Sesion", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });

        if (estado == EstadoSesion.Programada)
        {
            participanteSesionId = Guid.Empty;
            return sesion;
        }
        if (estado == EstadoSesion.Cancelada)
        {
            sesion.Preparar();
            sesion.Cancelar();
            participanteSesionId = Guid.Empty;
            return sesion;
        }

        sesion.Preparar();
        participanteSesionId = sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc).Id;
        if (estado == EstadoSesion.EnPreparacion) return sesion;

        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        return sesion;
    }

    private static SesionGrupal SesionGrupalEnEstado(EstadoSesion estado, out Guid equipoId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesion", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;

        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        return sesion;
    }
}
