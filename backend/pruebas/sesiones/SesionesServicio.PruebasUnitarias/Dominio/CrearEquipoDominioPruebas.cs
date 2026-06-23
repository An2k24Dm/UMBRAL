using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU40 — Reglas de creación de equipo (público/privado, capacidad, contraseña)
// directamente sobre el agregado SesionGrupal.
public class CrearEquipoDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionGrupal Crear(
        int maximoEquipos = 5, int maximoParticipantesPorEquipo = 2)
        => SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos, maximoParticipantesPorEquipo);

    private static readonly ContrasenaEquipoHash HashDemo =
        ContrasenaEquipoHash.Crear("$hash-demo$");

    [Fact]
    public void CrearEquipoPublico_SinContrasena_QuedaConHashNull()
    {
        var sesion = Crear();

        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.Tipo.Should().Be(TipoEquipo.Publico);
        equipo.ContrasenaHash.Should().BeNull();
        equipo.Puntaje.Should().Be(0);
    }

    [Fact]
    public void CrearEquipoPrivado_ConHash_GuardaContrasenaHash()
    {
        var sesion = Crear();

        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Privado, HashDemo,
            Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.Tipo.Should().Be(TipoEquipo.Privado);
        equipo.ContrasenaHash.Should().Be(HashDemo);
    }

    [Fact]
    public void CrearEquipoPrivado_SinHash_Lanza()
    {
        var sesion = Crear();

        Action accion = () => sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Privado, null,
            Guid.NewGuid(), AhoraUtc, AhoraUtc);

        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void CrearEquipoPublico_ConHash_IgnoraElHash()
    {
        var sesion = Crear();

        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, HashDemo,
            Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.ContrasenaHash.Should().BeNull();
    }

    [Fact]
    public void CrearEquipo_CapacidadCoincideConMaximoParticipantesPorEquipo()
    {
        var sesion = Crear(maximoParticipantesPorEquipo: 4);

        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.CapacidadMaxima.Should().Be(4);
    }

    [Fact]
    public void CrearEquipo_LiderQuedaComoParticipanteYLider()
    {
        var sesion = Crear();
        var lider = Guid.NewGuid();

        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            lider, AhoraUtc, AhoraUtc);

        equipo.Participantes.Should().ContainSingle(
            p => p.ParticipanteIdentidadId == lider);
        equipo.LiderParticipanteId.Should().Be(equipo.Participantes[0].Id);
    }
}
