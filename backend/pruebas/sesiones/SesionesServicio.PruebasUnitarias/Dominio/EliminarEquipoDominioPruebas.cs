using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU42 — Reglas de SesionGrupal.EliminarEquipo.
public class EliminarEquipoDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Miembro = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static SesionGrupal CrearSesion(int maximoEquipos = 5)
        => SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos, maximoParticipantesPorEquipo: 3);

    private static SesionGrupal SesionEnPreparacionConEquipo(
        Guid lider, out Guid equipoId, bool conMiembro = false)
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, lider, AhoraUtc, AhoraUtc);
        if (conMiembro)
            sesion.AgregarParticipanteAEquipo(equipo.Id, Miembro, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        sesion.Preparar();
        return sesion;
    }

    [Fact]
    public void Lider_EliminaEquipo_YaNoAparece()
    {
        var sesion = SesionEnPreparacionConEquipo(Lider, out var equipoId);

        sesion.EliminarEquipo(equipoId, Lider);

        sesion.Equipos.Should().BeEmpty();
    }

    [Fact]
    public void EliminarEquipo_EliminaTambienLosParticipantes()
    {
        var sesion = SesionEnPreparacionConEquipo(Lider, out var equipoId, conMiembro: true);

        sesion.EliminarEquipo(equipoId, Lider);

        // Ya no quedan equipos ni, por tanto, participantes asociados.
        sesion.Equipos.Should().BeEmpty();
    }

    [Fact]
    public void EliminarEquipo_LiberaCupo_PermiteCrearOtro()
    {
        // MaximoEquipos = 1: tras eliminar debe poder crearse uno nuevo.
        var sesion = CrearSesion(maximoEquipos: 1);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        sesion.Preparar();

        sesion.EliminarEquipo(equipo.Id, Lider);

        Action accion = () => sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        accion.Should().NotThrow();
        sesion.Equipos.Should().ContainSingle(e => e.Nombre.Valor == "Azul");
    }

    [Fact]
    public void NoLider_Rechaza()
    {
        var sesion = SesionEnPreparacionConEquipo(Lider, out var equipoId, conMiembro: true);

        // Miembro pertenece al equipo pero no es líder.
        Action accion = () => sesion.EliminarEquipo(equipoId, Miembro);
        accion.Should().Throw<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public void NoPertenece_Rechaza()
    {
        var sesion = SesionEnPreparacionConEquipo(Lider, out var equipoId);

        Action accion = () => sesion.EliminarEquipo(equipoId, Guid.NewGuid());
        accion.Should().Throw<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public void EquipoInexistente_Rechaza()
    {
        var sesion = SesionEnPreparacionConEquipo(Lider, out _);

        Action accion = () => sesion.EliminarEquipo(Guid.NewGuid(), Lider);
        accion.Should().Throw<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public void SesionNoEnPreparacion_Rechaza()
    {
        // No se llama a Preparar(): la sesión queda Programada.
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);

        Action accion = () => sesion.EliminarEquipo(equipo.Id, Lider);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void NoEliminaOtrosEquipos()
    {
        var sesion = CrearSesion();
        var rojo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        var azul = sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.Preparar();

        sesion.EliminarEquipo(rojo.Id, Lider);

        sesion.Equipos.Should().ContainSingle(e => e.Id == azul.Id);
    }

    [Fact]
    public void NoModifica_CapacidadNiEstadoNiMisiones()
    {
        var sesion = CrearSesion();
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        sesion.Preparar();
        var maxEquipos = sesion.MaximoEquipos;
        var maxPorEquipo = sesion.MaximoParticipantesPorEquipo;
        var misiones = sesion.Misiones.Count;

        sesion.EliminarEquipo(equipo.Id, Lider);

        sesion.MaximoEquipos.Should().Be(maxEquipos);
        sesion.MaximoParticipantesPorEquipo.Should().Be(maxPorEquipo);
        sesion.Misiones.Count.Should().Be(misiones);
        sesion.Estado.Should().Be(EstadoSesion.EnPreparacion);
    }
}
