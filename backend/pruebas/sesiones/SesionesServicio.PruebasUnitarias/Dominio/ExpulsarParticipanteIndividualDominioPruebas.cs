using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU44 — Reglas de SesionIndividual.ExpulsarParticipante.
public class ExpulsarParticipanteIndividualDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Participante = Guid.Parse("55555555-5555-5555-5555-555555555555");

    // Crea una sesión individual con un participante y la deja en el estado
    // pedido. El participante se agrega En Preparación (única ventana válida).
    private static SesionIndividual SesionConParticipante(
        out Guid participanteSesionId, EstadoSesion estado = EstadoSesion.EnPreparacion)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        sesion.Preparar();
        participanteSesionId = sesion.AgregarParticipante(Participante, AhoraUtc).Id;

        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Cancelada) { sesion.Cancelar(); return sesion; }
        return sesion;
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Pausada)]
    public void EstadoPermitido_ExpulsaParticipante(EstadoSesion estado)
    {
        var sesion = SesionConParticipante(out var participanteSesionId, estado);

        sesion.ExpulsarParticipante(participanteSesionId);

        sesion.Participantes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void EstadoNoPermitido_Rechaza(EstadoSesion estado)
    {
        var sesion = SesionConParticipante(out var participanteSesionId, estado);

        Action accion = () => sesion.ExpulsarParticipante(participanteSesionId);

        accion.Should().Throw<ExpulsionNoPermitidaExcepcion>();
        sesion.Participantes.Should().ContainSingle(p => p.Id == participanteSesionId);
    }

    [Fact]
    public void Programada_Rechaza()
    {
        // En Programada no hay participantes: la regla de estado se valida
        // antes de buscar al participante.
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);

        Action accion = () => sesion.ExpulsarParticipante(Guid.NewGuid());

        accion.Should().Throw<ExpulsionNoPermitidaExcepcion>();
    }

    [Fact]
    public void ParticipanteInexistente_Rechaza()
    {
        var sesion = SesionConParticipante(out _, EstadoSesion.EnPreparacion);

        Action accion = () => sesion.ExpulsarParticipante(Guid.NewGuid());

        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
    }

    [Fact]
    public void ExpulsarUno_NoAfectaOtros()
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        sesion.Preparar();
        var primero = sesion.AgregarParticipante(Participante, AhoraUtc).Id;
        var segundo = sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc).Id;

        sesion.ExpulsarParticipante(primero);

        sesion.Participantes.Should().ContainSingle(p => p.Id == segundo);
    }
}
