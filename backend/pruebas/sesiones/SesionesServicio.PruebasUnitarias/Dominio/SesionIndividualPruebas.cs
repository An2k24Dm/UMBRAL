using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Factorias;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class SesionIndividualPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionIndividual Crear()
        => FabricaSesiones.CrearIndividual(
            "Sesión piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc);

    [Fact]
    public void AgregarParticipante_LoIncluyeConDatosCorrectos()
    {
        var sesion = Crear();
        var pid = Guid.NewGuid();

        var p = sesion.AgregarParticipante(pid, AhoraUtc);

        p.ParticipanteIdentidadId.Should().Be(pid);
        p.EquipoId.Should().BeNull();
        p.FechaUnionSesion.Should().Be(AhoraUtc);
        p.FechaUnionEquipo.Should().BeNull();
        p.Puntaje.Should().Be(0);
        sesion.Participantes.Should().HaveCount(1);
    }

    [Fact]
    public void AgregarParticipante_IdVacio_Lanza()
    {
        var sesion = Crear();
        Action accion = () => sesion.AgregarParticipante(Guid.Empty, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AgregarParticipante_Duplicado_Lanza()
    {
        var sesion = Crear();
        var pid = Guid.NewGuid();
        sesion.AgregarParticipante(pid, AhoraUtc);

        Action accion = () => sesion.AgregarParticipante(pid, AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AgregarParticipante_MasDelMaximo_Lanza()
    {
        var sesion = Crear();
        for (var i = 0; i < PoliticaCapacidadSesion.MaximoParticipantesIndividual; i++)
            sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);

        Action accion = () => sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_AceptaEntre1Y5()
    {
        var sesion = Crear();
        var lista = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        sesion.AsignarMisiones(lista);
        sesion.Misiones.Should().HaveCount(5);
    }
}
