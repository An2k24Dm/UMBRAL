using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU41 — Reglas de SesionGrupal.ModificarEquipo / Equipo.ModificarDatos.
public class ModificarEquipoDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static readonly ContrasenaEquipoHash HashPrevio =
        ContrasenaEquipoHash.Crear("hash-previo");
    private static readonly ContrasenaEquipoHash HashNuevo =
        ContrasenaEquipoHash.Crear("hash-nuevo");

    private static SesionGrupal CrearSesion()
        => SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 3);

    private static SesionGrupal SesionConEquipo(
        Guid lider, out Guid equipoId,
        TipoEquipo tipo = TipoEquipo.Publico,
        ContrasenaEquipoHash? hash = null)
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), tipo, hash, lider, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        sesion.Preparar(); // Programada -> EnPreparacion
        return sesion;
    }

    [Fact]
    public void Lider_CambiaNombre()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId);

        sesion.ModificarEquipo(equipoId, Lider, NombreEquipo.Crear("Azul"),
            TipoEquipo.Publico, null, false);

        sesion.Equipos.Single().Nombre.Valor.Should().Be("Azul");
    }

    [Fact]
    public void Lider_CambiaPublicoAPrivado_ConHash()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId);

        var equipo = sesion.ModificarEquipo(equipoId, Lider, NombreEquipo.Crear("Rojo"),
            TipoEquipo.Privado, HashNuevo, true);

        equipo.Tipo.Should().Be(TipoEquipo.Privado);
        equipo.ContrasenaHash.Should().Be(HashNuevo);
    }

    [Fact]
    public void Lider_CambiaPrivadoAPublico_LimpiaContrasena()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId, TipoEquipo.Privado, HashPrevio);

        var equipo = sesion.ModificarEquipo(equipoId, Lider, NombreEquipo.Crear("Rojo"),
            TipoEquipo.Publico, null, false);

        equipo.Tipo.Should().Be(TipoEquipo.Publico);
        equipo.ContrasenaHash.Should().BeNull();
    }

    [Fact]
    public void Lider_CambiaContrasenaPrivada()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId, TipoEquipo.Privado, HashPrevio);

        var equipo = sesion.ModificarEquipo(equipoId, Lider, NombreEquipo.Crear("Rojo"),
            TipoEquipo.Privado, HashNuevo, true);

        equipo.ContrasenaHash.Should().Be(HashNuevo);
    }

    [Fact]
    public void Lider_PrivadoSinNuevaContrasena_ConservaHash()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId, TipoEquipo.Privado, HashPrevio);

        var equipo = sesion.ModificarEquipo(equipoId, Lider, NombreEquipo.Crear("Nuevo"),
            TipoEquipo.Privado, null, false);

        equipo.Nombre.Valor.Should().Be("Nuevo");
        equipo.ContrasenaHash.Should().Be(HashPrevio);
    }

    [Fact]
    public void NoLider_Rechaza()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId);
        var otro = Guid.NewGuid();

        Action accion = () => sesion.ModificarEquipo(equipoId, otro,
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, false);

        accion.Should().Throw<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public void NombreDuplicado_Rechaza()
    {
        var sesion = CrearSesion();
        var rojo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.Preparar();

        Action accion = () => sesion.ModificarEquipo(rojo.Id, Lider,
            NombreEquipo.Crear("azul"), TipoEquipo.Publico, null, false);

        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void PublicoAPrivadoSinHash_Rechaza()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId);

        Action accion = () => sesion.ModificarEquipo(equipoId, Lider,
            NombreEquipo.Crear("Rojo"), TipoEquipo.Privado, null, false);

        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void SesionNoEnPreparacion_Rechaza()
    {
        // No se llama a Preparar(): la sesión queda Programada.
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);

        Action accion = () => sesion.ModificarEquipo(equipo.Id, Lider,
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, false);

        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void EquipoInexistente_Rechaza()
    {
        var sesion = SesionConEquipo(Lider, out _);

        Action accion = () => sesion.ModificarEquipo(Guid.NewGuid(), Lider,
            NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null, false);

        accion.Should().Throw<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public void NoModifica_LiderPuntajeNiCapacidad()
    {
        var sesion = SesionConEquipo(Lider, out var equipoId);
        var antes = sesion.Equipos.Single();
        var liderId = antes.LiderParticipanteId;
        var capacidad = antes.CapacidadMaxima;
        var puntaje = antes.Puntaje;
        var integrantes = antes.Participantes.Count;

        var equipo = sesion.ModificarEquipo(equipoId, Lider, NombreEquipo.Crear("Azul"),
            TipoEquipo.Publico, null, false);

        equipo.LiderParticipanteId.Should().Be(liderId);
        equipo.CapacidadMaxima.Should().Be(capacidad);
        equipo.Puntaje.Should().Be(puntaje);
        equipo.Participantes.Count.Should().Be(integrantes);
    }
}
