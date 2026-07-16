using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class EquipoPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const int MaximoParticipantesPorEquipo = 2;

    private static SesionGrupal CrearSesion()
        => SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: MaximoParticipantesPorEquipo);

    [Fact]
    public void CrearEquipo_LiderEsIntegrante()
    {
        var sesion = CrearSesion();
        var lider = Guid.NewGuid();
        var equipo = sesion.CrearEquipo("Rojo", lider, AhoraUtc, AhoraUtc);

        equipo.Participantes.Should().ContainSingle(
            p => p.ParticipanteIdentidadId == lider);
        equipo.LiderParticipanteId.Should().Be(equipo.Participantes[0].Id);
    }

    [Fact]
    public void EquipoNace_ConPuntajeCero()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.Puntaje.Valor.Should().Be(0);
    }

    [Fact]
    public void EstaLleno_RefleyaCapacidad()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.EstaLleno().Should().BeFalse();

        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        equipo.EstaLleno().Should().BeTrue();
    }

    // El puntaje del equipo es derivado: se suma a los participantes y el
    // total del equipo se recalcula como la suma de sus integrantes.
    [Fact]
    public void SumarPuntajeAParticipante_RecalculaPuntajeDelEquipo()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var lider = equipo.Participantes[0];
        var integrante = equipo.Participantes[1];

        equipo.SumarPuntajeAParticipante(lider.Id, 10);
        equipo.SumarPuntajeAParticipante(integrante.Id, 5);

        lider.Puntaje.Valor.Should().Be(10);
        integrante.Puntaje.Valor.Should().Be(5);
        equipo.Puntaje.Valor.Should().Be(15);
    }

    [Fact]
    public void SumarPuntajeAParticipante_ElEquipoSiempreEsLaSumaDeSusIntegrantes()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc);

        equipo.SumarPuntajeAParticipante(equipo.Participantes[0].Id, 7);
        equipo.SumarPuntajeAParticipante(equipo.Participantes[1].Id, 3);
        equipo.SumarPuntajeAParticipante(equipo.Participantes[0].Id, 5);

        equipo.Puntaje.Valor.Should().Be(
            equipo.Participantes.Sum(p => p.Puntaje.Valor));
    }

    [Fact]
    public void SumarPuntajeAParticipante_NegativoLanza()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        Action accion = () => equipo.SumarPuntajeAParticipante(
            equipo.LiderParticipanteId, -1);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void SumarPuntajeAParticipante_ParticipanteAjenoLanza()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        Action accion = () => equipo.SumarPuntajeAParticipante(Guid.NewGuid(), 10);
        accion.Should().Throw<ParticipanteNoEncontradoExcepcion>();
    }

    [Fact]
    public void CrearEquipoPrivado_SinContrasena_Lanza()
    {
        var sesion = CrearSesion();

        Action accion = () => sesion.CrearEquipo(
            NombreEquipo.Crear("Privado"),
            TipoEquipo.Privado,
            null,
            Guid.NewGuid(),
            AhoraUtc,
            AhoraUtc);

        accion.Should().Throw<EquipoInvalidoExcepcion>()
            .WithMessage("*contraseña*");
    }

    [Fact]
    public void ModificarEquipo_PublicoLimpiaContrasenaYPrivadoExigeHash()
    {
        var sesion = CrearSesion();
        sesion.Preparar();
        var liderIdentidadId = Guid.NewGuid();
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Privado"),
            TipoEquipo.Privado,
            ContrasenaEquipoHash.Crear("hash-1"),
            liderIdentidadId,
            AhoraUtc,
            AhoraUtc);

        sesion.ModificarEquipo(
            equipo.Id,
            liderIdentidadId,
            NombreEquipo.Crear("Publico"),
            TipoEquipo.Publico,
            null,
            actualizarContrasena: false);

        equipo.Tipo.Should().Be(TipoEquipo.Publico);
        equipo.ContrasenaHash.Should().BeNull();

        Action privadoSinHash = () => sesion.ModificarEquipo(
            equipo.Id,
            liderIdentidadId,
            NombreEquipo.Crear("Privado otra vez"),
            TipoEquipo.Privado,
            null,
            actualizarContrasena: false);
        privadoSinHash.Should().Throw<EquipoInvalidoExcepcion>()
            .WithMessage("*contraseña*");

        sesion.ModificarEquipo(
            equipo.Id,
            liderIdentidadId,
            NombreEquipo.Crear("Privado otra vez"),
            TipoEquipo.Privado,
            ContrasenaEquipoHash.Crear("hash-2"),
            actualizarContrasena: true);

        equipo.Tipo.Should().Be(TipoEquipo.Privado);
        equipo.ContrasenaHash.Should().NotBeNull();
    }

    [Fact]
    public void ExpulsarLider_ReasignaAlIntegranteMasAntiguo()
    {
        var sesion = CrearSesion();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var segundo = sesion.AgregarParticipanteAEquipo(
            equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc.AddMinutes(1));
        sesion.Preparar();

        var expulsado = sesion.ExpulsarParticipanteDeEquipo(
            equipo.Id,
            equipo.LiderParticipanteId,
            actorParticipanteIdentidadId: Guid.NewGuid(),
            actorEsOperador: true);

        expulsado.Id.Should().NotBe(segundo.Id);
        equipo.LiderParticipanteId.Should().Be(segundo.Id);
        equipo.Participantes.Should().ContainSingle(p => p.Id == segundo.Id);
    }

    [Fact]
    public void Rehidratar_RestauraDatosPersistidosYNormalizaContrasenaSegunTipo()
    {
        var lider = Guid.NewGuid();
        var integrante = Participante.Rehidratar(
            lider, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            puntaje: 15,
            fechaUnionSesion: AhoraUtc,
            fechaUnionEquipo: AhoraUtc,
            snapshotRankingUtc: AhoraUtc.AddMinutes(1));

        var privado = Equipo.Rehidratar(
            Guid.NewGuid(),
            integrante.SesionId,
            "Rojo",
            lider,
            puntaje: 40,
            TipoEquipo.Privado,
            "hash-privado",
            capacidadMaxima: 3,
            AhoraUtc,
            new[] { integrante },
            snapshotRankingUtc: AhoraUtc.AddMinutes(2));
        var publico = Equipo.Rehidratar(
            Guid.NewGuid(),
            integrante.SesionId,
            "Azul",
            lider,
            puntaje: 10,
            TipoEquipo.Publico,
            "hash-ignorado",
            capacidadMaxima: 3,
            AhoraUtc);

        privado.Puntaje.Valor.Should().Be(40);
        privado.ContrasenaHash.Should().NotBeNull();
        privado.Participantes.Should().ContainSingle();
        privado.SnapshotRankingUtc.Should().Be(AhoraUtc.AddMinutes(2));
        publico.ContrasenaHash.Should().BeNull();
    }
}
