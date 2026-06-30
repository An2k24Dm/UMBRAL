using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU44 — Reglas de SesionGrupal.ExpulsarEquipo (acción del Operador).
public class ExpulsarEquipoGrupalDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Miembro = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static SesionGrupal SesionConEquipo(
        out Guid equipoId, EstadoSesion estado = EstadoSesion.EnPreparacion,
        bool conMiembro = false)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        if (conMiembro)
            sesion.AgregarParticipanteAEquipo(equipo.Id, Miembro, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;

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
    public void EstadoPermitido_ExpulsaEquipo(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, estado);

        sesion.ExpulsarEquipo(equipoId);

        sesion.Equipos.Should().BeEmpty();
    }

    [Fact]
    public void ExpulsarEquipo_SusParticipantesDejanDePertenecer()
    {
        var sesion = SesionConEquipo(out var equipoId, EstadoSesion.EnPreparacion, conMiembro: true);

        sesion.ExpulsarEquipo(equipoId);

        // Sin equipos no quedan participantes grupales asociados a la sesión.
        sesion.Equipos.SelectMany(e => e.Participantes).Should().BeEmpty();
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void EstadoNoPermitido_Rechaza(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, estado);

        Action accion = () => sesion.ExpulsarEquipo(equipoId);

        accion.Should().Throw<ExpulsionNoPermitidaExcepcion>();
        sesion.Equipos.Should().ContainSingle(e => e.Id == equipoId);
    }

    [Fact]
    public void EquipoInexistente_Rechaza()
    {
        var sesion = SesionConEquipo(out _, EstadoSesion.EnPreparacion);

        Action accion = () => sesion.ExpulsarEquipo(Guid.NewGuid());

        accion.Should().Throw<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public void ExpulsarUno_NoAfectaOtrosEquipos()
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        var rojo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        var azul = sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.Preparar();

        sesion.ExpulsarEquipo(rojo.Id);

        sesion.Equipos.Should().ContainSingle(e => e.Id == azul.Id);
    }
}
