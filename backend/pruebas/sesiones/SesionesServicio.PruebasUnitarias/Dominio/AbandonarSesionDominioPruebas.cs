using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU48 — Abandono voluntario: SesionIndividual.AbandonarSesion,
// SesionGrupal.AbandonarEquipo y Equipo.AbandonarParticipante. Solo se
// permite En Preparación (a diferencia de las expulsiones HU44/45).
public class AbandonarSesionDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LiderIdentidad = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid MiembroIdentidad = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // ---- Sesión individual ----

    private static SesionIndividual IndividualConParticipante(
        EstadoSesion estado = EstadoSesion.EnPreparacion)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 2);
        sesion.Preparar();
        sesion.AgregarParticipante(MiembroIdentidad, AhoraUtc);

        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Cancelada) { sesion.Cancelar(); return sesion; }
        return sesion;
    }

    [Fact]
    public void Individual_Abandona_EliminaYLiberaCupo()
    {
        var sesion = IndividualConParticipante();

        var removido = sesion.AbandonarSesion(MiembroIdentidad);

        removido.ParticipanteIdentidadId.Should().Be(MiembroIdentidad);
        sesion.Participantes.Should().BeEmpty();
        // Cupo liberado: otro participante puede ingresar.
        Action otro = () => sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);
        otro.Should().NotThrow();
    }

    [Fact]
    public void Individual_NoPertenece_Rechaza()
    {
        var sesion = IndividualConParticipante();

        Action accion = () => sesion.AbandonarSesion(Guid.NewGuid());

        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
        sesion.Participantes.Should().ContainSingle();
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void Individual_EstadoNoPermitido_Rechaza(EstadoSesion estado)
    {
        var sesion = IndividualConParticipante(estado);

        Action accion = () => sesion.AbandonarSesion(MiembroIdentidad);

        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
        sesion.Participantes.Should().ContainSingle();
    }

    [Fact]
    public void Individual_Programada_Rechaza()
    {
        // En Programada no hay participantes: la regla de estado se valida
        // antes de buscar la participación.
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 2);

        Action accion = () => sesion.AbandonarSesion(MiembroIdentidad);

        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    // ---- Sesión grupal / equipo ----

    private static SesionGrupal GrupalConEquipo(
        out Guid equipoId,
        EstadoSesion estado = EstadoSesion.EnPreparacion,
        bool conMiembro = true)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            LiderIdentidad, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        if (conMiembro)
            sesion.AgregarParticipanteAEquipo(
                equipo.Id, MiembroIdentidad, AhoraUtc.AddMinutes(1), AhoraUtc.AddMinutes(1));

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
    public void Grupal_IntegranteNormalAbandona_LiderSeMantiene_LiberaCupo()
    {
        var sesion = GrupalConEquipo(out var equipoId);
        var equipo = sesion.Equipos.Single();
        var liderSesionId = equipo.LiderParticipanteId;

        var removido = sesion.AbandonarEquipo(MiembroIdentidad);

        removido.ParticipanteIdentidadId.Should().Be(MiembroIdentidad);
        removido.EquipoId.Should().Be(equipoId);
        equipo.LiderParticipanteId.Should().Be(liderSesionId);
        equipo.Participantes.Should().ContainSingle(
            p => p.ParticipanteIdentidadId == LiderIdentidad);
        // Cupo liberado en el equipo.
        Action otro = () => sesion.AgregarParticipanteAEquipo(
            equipoId, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        otro.Should().NotThrow();
    }

    [Fact]
    public void Grupal_LiderAbandonaConOtros_ReasignaAlSiguiente()
    {
        var sesion = GrupalConEquipo(out var equipoId);
        var miembroSesionId = sesion.Equipos.Single().Participantes
            .Single(p => p.ParticipanteIdentidadId == MiembroIdentidad).Id;

        var removido = sesion.AbandonarEquipo(LiderIdentidad);

        removido.ParticipanteIdentidadId.Should().Be(LiderIdentidad);
        var equipo = sesion.Equipos.Single();
        equipo.LiderParticipanteId.Should().Be(miembroSesionId);
        equipo.EsLider(MiembroIdentidad).Should().BeTrue();
    }

    [Fact]
    public void Grupal_LiderUnicoAbandona_EliminaElEquipo()
    {
        var sesion = GrupalConEquipo(out var equipoId, conMiembro: false);

        var removido = sesion.AbandonarEquipo(LiderIdentidad);

        removido.EquipoId.Should().Be(equipoId);
        sesion.Equipos.Should().BeEmpty();
        // Cupo de equipos liberado: puede crearse otro equipo.
        Action otro = () => sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null,
            Guid.NewGuid(), AhoraUtc, AhoraUtc);
        otro.Should().NotThrow();
    }

    [Fact]
    public void Grupal_NoPerteneceANingunEquipo_Rechaza()
    {
        var sesion = GrupalConEquipo(out _);

        Action accion = () => sesion.AbandonarEquipo(Guid.NewGuid());

        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void Grupal_EstadoNoPermitido_Rechaza(EstadoSesion estado)
    {
        var sesion = GrupalConEquipo(out _, estado);

        Action accion = () => sesion.AbandonarEquipo(MiembroIdentidad);

        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
        sesion.Equipos.Single().Participantes.Should().HaveCount(2);
    }
}
