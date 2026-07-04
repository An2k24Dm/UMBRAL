using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class ParticipantePruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CrearParaSesionIndividual_EquipoIdNuloYPuntaje0()
    {
        var sesionId = Guid.NewGuid();
        var pid = Guid.NewGuid();

        var p = Participante.CrearParaSesionIndividual(sesionId, pid, AhoraUtc);

        p.SesionId.Should().Be(sesionId);
        p.ParticipanteIdentidadId.Should().Be(pid);
        p.EquipoId.Should().BeNull();
        p.FechaUnionEquipo.Should().BeNull();
        p.FechaUnionSesion.Should().Be(AhoraUtc);
        p.Puntaje.Valor.Should().Be(0);
    }

    [Fact]
    public void CrearParaEquipo_AsignaEquipoIdYFechas()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var union = AhoraUtc.AddMinutes(1);

        var p = Participante.CrearParaEquipo(sesionId, equipoId, pid, AhoraUtc, union);

        p.EquipoId.Should().Be(equipoId);
        p.FechaUnionSesion.Should().Be(AhoraUtc);
        p.FechaUnionEquipo.Should().Be(union);
        p.Puntaje.Valor.Should().Be(0);
    }

    [Fact]
    public void CrearParaSesionIndividual_SesionVacia_Lanza()
    {
        Action accion = () => Participante.CrearParaSesionIndividual(
            Guid.Empty, Guid.NewGuid(), AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void CrearParaSesionIndividual_IdentidadVacia_Lanza()
    {
        Action accion = () => Participante.CrearParaSesionIndividual(
            Guid.NewGuid(), Guid.Empty, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void SumarPuntaje_AcumulaCorrectamente()
    {
        var p = Participante.CrearParaSesionIndividual(Guid.NewGuid(), Guid.NewGuid(), AhoraUtc);

        p.SumarPuntaje(10);
        p.SumarPuntaje(5);

        p.Puntaje.Valor.Should().Be(15);
    }

    [Fact]
    public void SumarPuntaje_NoAceptaNegativo()
    {
        var p = Participante.CrearParaSesionIndividual(Guid.NewGuid(), Guid.NewGuid(), AhoraUtc);
        Action accion = () => p.SumarPuntaje(-1);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }
}
