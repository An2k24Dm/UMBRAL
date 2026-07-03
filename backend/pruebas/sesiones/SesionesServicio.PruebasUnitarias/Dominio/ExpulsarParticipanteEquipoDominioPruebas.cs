using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU45 — Reglas de SesionGrupal.ExpulsarParticipanteDeEquipo y de la
// reasignación de liderazgo en Equipo.ExpulsarParticipante.
public class ExpulsarParticipanteEquipoDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LiderIdentidad = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid MiembroIdentidad = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // Crea una sesión grupal con un equipo (líder + opcionalmente un miembro)
    // y la deja en el estado pedido.
    private static SesionGrupal SesionConEquipo(
        out Guid equipoId, out Guid liderSesionId, out Guid miembroSesionId,
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
        liderSesionId = equipo.LiderParticipanteId;
        miembroSesionId = Guid.Empty;
        if (conMiembro)
            miembroSesionId = sesion.AgregarParticipanteAEquipo(
                equipo.Id, MiembroIdentidad, AhoraUtc.AddMinutes(1), AhoraUtc.AddMinutes(1)).Id;

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

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Pausada)]
    public void Lider_ExpulsaIntegranteNormal_EnEstadosPermitidos(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId, estado);

        var expulsado = sesion.ExpulsarParticipanteDeEquipo(
            equipoId, miembroId, LiderIdentidad, actorEsOperador: false);

        expulsado.ParticipanteIdentidadId.Should().Be(MiembroIdentidad);
        var equipo = sesion.Equipos.Single();
        equipo.Participantes.Should().ContainSingle(
            p => p.ParticipanteIdentidadId == LiderIdentidad);
    }

    [Fact]
    public void Lider_NoPuedeExpulsarseASiMismo_NiAlLider()
    {
        var sesion = SesionConEquipo(out var equipoId, out var liderSesionId, out _);

        Action accion = () => sesion.ExpulsarParticipanteDeEquipo(
            equipoId, liderSesionId, LiderIdentidad, actorEsOperador: false);

        accion.Should().Throw<EquipoInvalidoExcepcion>()
            .WithMessage("No puedes expulsar al líder del equipo.");
    }

    [Fact]
    public void NoLider_NoPuedeExpulsar()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);

        // El miembro (no líder) intenta expulsar al líder o a otro.
        Action accion = () => sesion.ExpulsarParticipanteDeEquipo(
            equipoId, miembroId, MiembroIdentidad, actorEsOperador: false);

        accion.Should().Throw<AccesoSesionNoPermitidoExcepcion>()
            .WithMessage("Solo el líder del equipo puede expulsar participantes.");
    }

    [Fact]
    public void Operador_ExpulsaIntegranteNormal()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);

        var expulsado = sesion.ExpulsarParticipanteDeEquipo(
            equipoId, miembroId, Operador, actorEsOperador: true);

        expulsado.Id.Should().Be(miembroId);
        sesion.Equipos.Single().Participantes.Should().HaveCount(1);
    }

    [Fact]
    public void Operador_ExpulsaLider_ReasignaAlSiguiente()
    {
        var sesion = SesionConEquipo(
            out var equipoId, out var liderSesionId, out var miembroId);

        var expulsado = sesion.ExpulsarParticipanteDeEquipo(
            equipoId, liderSesionId, Operador, actorEsOperador: true);

        expulsado.ParticipanteIdentidadId.Should().Be(LiderIdentidad);
        var equipo = sesion.Equipos.Single();
        // El siguiente integrante (por fecha de unión) queda como líder.
        equipo.LiderParticipanteId.Should().Be(miembroId);
        equipo.EsLider(MiembroIdentidad).Should().BeTrue();
        equipo.Participantes.Should().ContainSingle(p => p.Id == miembroId);
    }

    [Fact]
    public void Operador_NoPuedeExpulsarUnicoIntegranteLider()
    {
        var sesion = SesionConEquipo(
            out var equipoId, out var liderSesionId, out _, conMiembro: false);

        Action accion = () => sesion.ExpulsarParticipanteDeEquipo(
            equipoId, liderSesionId, Operador, actorEsOperador: true);

        accion.Should().Throw<EquipoInvalidoExcepcion>()
            .WithMessage("*único integrante*");
        sesion.Equipos.Single().Participantes.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void EstadoNoPermitido_Rechaza(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId, estado);

        Action accion = () => sesion.ExpulsarParticipanteDeEquipo(
            equipoId, miembroId, Operador, actorEsOperador: true);

        accion.Should().Throw<ExpulsionNoPermitidaExcepcion>();
        sesion.Equipos.Single().Participantes.Should().HaveCount(2);
    }

    [Fact]
    public void EquipoInexistente_Rechaza()
    {
        var sesion = SesionConEquipo(out _, out _, out var miembroId);

        Action accion = () => sesion.ExpulsarParticipanteDeEquipo(
            Guid.NewGuid(), miembroId, Operador, actorEsOperador: true);

        accion.Should().Throw<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public void ParticipanteQueNoPertenece_Rechaza()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out _);

        Action accion = () => sesion.ExpulsarParticipanteDeEquipo(
            equipoId, Guid.NewGuid(), Operador, actorEsOperador: true);

        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
    }
}
