using System;
using FluentAssertions;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;
using Xunit;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU52 — Reglas de dominio de penalización en Sesiones: CantidadPenalizacion,
// PuntajeSesion negativo, entidad PenalizacionSesion, validación de estado y
// objetivo, y snapshots de penalización.
public sealed class PenalizacionSesionDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // -------------------- CantidadPenalizacion --------------------

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
    {
        var accion = () => CantidadPenalizacion.Crear(valor);
        accion.Should().Throw<PenalizacionInvalidaExcepcion>();
    }

    // -------------------- PuntajeSesion negativo --------------------

    [Fact]
    public void PuntajeSesion_desdePersistencia_admiteNegativo()
        => PuntajeSesion.DesdePersistencia(-5).Valor.Should().Be(-5);

    [Fact]
    public void PuntajeSesion_ganado_sigueSinNegativos()
    {
        var accionCrear = () => PuntajeSesion.Crear(-1);
        var accionSumar = () => PuntajeSesion.Cero().Sumar(-1);
        accionCrear.Should().Throw<ParticipacionInvalidaExcepcion>();
        accionSumar.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    // -------------------- Entidad PenalizacionSesion --------------------

    [Fact]
    public void CrearParaParticipante_registraOrigenMotivoMomentoOperadorYObjetivo()
    {
        var eventoId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        var penalizacion = PenalizacionSesion.CrearParaParticipante(
            eventoId, sesionId, participanteSesionId, identidadId,
            5, "  Incumplió una regla  ", Operador, AhoraUtc);

        penalizacion.EventoId.Should().Be(eventoId);
        penalizacion.SesionId.Should().Be(sesionId);
        penalizacion.TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Participante);
        penalizacion.ParticipanteSesionId.Should().Be(participanteSesionId);
        penalizacion.ParticipanteIdentidadId.Should().Be(identidadId);
        penalizacion.EquipoId.Should().BeNull();
        penalizacion.Puntos.Should().Be(5);
        penalizacion.Motivo.Should().Be("Incumplió una regla"); // Trim aplicado
        penalizacion.OperadorIdentidadId.Should().Be(Operador);
        penalizacion.AplicadaEnUtc.Should().Be(AhoraUtc);
        penalizacion.EstadoProcesamiento.Should().Be(EstadoProcesamientoPenalizacion.Pendiente);
        penalizacion.ProcesadaEnUtc.Should().BeNull();
        penalizacion.PuntajeResultante.Should().BeNull();
    }

    [Fact]
    public void CrearParaEquipo_dejaParticipanteEnNull()
    {
        var equipoId = Guid.NewGuid();

        var penalizacion = PenalizacionSesion.CrearParaEquipo(
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
    {
        var penalizacion = PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            puntos, "Motivo", Operador, AhoraUtc);
        penalizacion.Puntos.Should().Be(puntos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(101)]
    public void CrearParaParticipante_rechazaPuntosFueraDeRango(int puntos)
    {
        var accion = () => PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            puntos, "Motivo", Operador, AhoraUtc);
        accion.Should().Throw<PenalizacionInvalidaExcepcion>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearParaParticipante_rechazaMotivoInvalido(string? motivo)
    {
        var accion = () => PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            5, motivo!, Operador, AhoraUtc);
        accion.Should().Throw<PenalizacionInvalidaExcepcion>();
    }

    [Fact]
    public void CrearParaParticipante_rechazaMotivoMayorA500()
    {
        var motivo = new string('a', 501);
        var accion = () => PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            5, motivo, Operador, AhoraUtc);
        accion.Should().Throw<PenalizacionInvalidaExcepcion>();
    }

    [Fact]
    public void CrearParaParticipante_aceptaMotivoDe500()
    {
        var motivo = new string('a', 500);
        var penalizacion = PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            5, motivo, Operador, AhoraUtc);
        penalizacion.Motivo.Length.Should().Be(500);
    }

    [Fact]
    public void MarcarProcesada_esIdempotente()
    {
        var penalizacion = PenalizacionSesion.CrearParaEquipo(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, "Motivo", Operador, AhoraUtc);

        penalizacion.MarcarProcesada(-3, AhoraUtc).Should().BeTrue();
        penalizacion.EstadoProcesamiento.Should().Be(EstadoProcesamientoPenalizacion.Procesada);
        penalizacion.PuntajeResultante.Should().Be(-3);
        penalizacion.ProcesadaEnUtc.Should().Be(AhoraUtc);

        // Segundo intento no cambia nada (idempotente).
        penalizacion.MarcarProcesada(99, AhoraUtc.AddMinutes(1)).Should().BeFalse();
        penalizacion.PuntajeResultante.Should().Be(-3);
    }

    // -------------------- Estado de la sesión --------------------

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public void ValidarPuedePenalizar_permiteActivaYPausada(EstadoSesion estado)
    {
        var sesion = SesionIndividualEnEstado(estado, out _);
        var accion = () => sesion.ValidarPuedePenalizar();
        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void ValidarPuedePenalizar_rechazaOtrosEstados(EstadoSesion estado)
    {
        var sesion = SesionIndividualEnEstado(estado, out _);
        var accion = () => sesion.ValidarPuedePenalizar();
        accion.Should().Throw<PenalizacionNoPermitidaExcepcion>();
    }

    [Fact]
    public void ObtenerParticipanteParaPenalizar_objetivoInexistente_lanza404()
    {
        var sesion = SesionIndividualEnEstado(EstadoSesion.Activa, out _);
        var accion = () => sesion.ObtenerParticipanteParaPenalizar(Guid.NewGuid());
        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
    }

    [Fact]
    public void ObtenerEquipoParaPenalizar_devuelveEquipoEnEstadoValido()
    {
        var sesion = SesionGrupalEnEstado(EstadoSesion.Pausada, out var equipoId);
        var equipo = sesion.ObtenerEquipoParaPenalizar(equipoId);
        equipo.Id.Should().Be(equipoId);
    }

    [Fact]
    public void ObtenerEquipoParaPenalizar_equipoInexistente_lanza404()
    {
        var sesion = SesionGrupalEnEstado(EstadoSesion.Activa, out _);
        var accion = () => sesion.ObtenerEquipoParaPenalizar(Guid.NewGuid());
        accion.Should().Throw<EquipoNoEncontradoExcepcion>();
    }

    // -------------------- Snapshots --------------------

    [Fact]
    public void Participante_establecerPenalizacionSnapshot_fijaNegativoYMagnitud()
    {
        var sesion = SesionIndividualEnEstado(EstadoSesion.Activa, out var participanteSesionId);
        var participante = sesion.Participantes.Single(p => p.Id == participanteSesionId);

        var aplicado = participante.EstablecerPenalizacionSnapshot(13, -3, AhoraUtc);

        aplicado.Should().BeTrue();
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

        var aplicado = participante.EstablecerPenalizacionSnapshot(99, 50, AhoraUtc.AddSeconds(-5));

        aplicado.Should().BeFalse();
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

    // -------------------- Helpers --------------------

    private static SesionIndividual SesionIndividualEnEstado(
        EstadoSesion estado, out Guid participanteSesionId)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });

        if (estado == EstadoSesion.Programada)
        {
            participanteSesionId = Guid.Empty;
            return sesion;
        }
        if (estado == EstadoSesion.Cancelada)
        {
            // Cancelar solo es válido desde EnPreparacion/Activa/Pausada.
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
            "Sesión", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
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
