using System;
using System.Collections.Generic;
using System.Linq;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.PruebasUnitarias.Validaciones;

// Reglas de validación del DTO de modificación (HU38). Espejo de las reglas
// de creación: datos básicos, misiones y capacidad por modo.
public class ValidadorModificarSesionPruebas
{
    private static readonly Guid SesionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ValidadorModificarSesion _validador = new();

    private static ModificarSesionDto Base(string modo) => new()
    {
        Nombre = "Sesión editada",
        Descripcion = "Demo",
        Modo = modo,
        FechaProgramada = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
        MisionesIds = new List<Guid> { Guid.NewGuid() }
    };

    private ResultadoValidacion Validar(ModificarSesionDto dto)
        => _validador.Validar(new ModificarSesionComando(SesionId, dto));

    [Fact]
    public void Individual_ConCapacidadValida_EsValido()
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = 10;

        Validar(dto).EsValido.Should().BeTrue();
    }

    [Fact]
    public void Individual_SinCapacidad_Falla()
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = null;

        Validar(dto).Errores.Should().Contain(e => e.Campo == "maximoParticipantes");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Individual_CapacidadMenorAUno_Falla(int maximo)
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = maximo;

        Validar(dto).Errores.Should().Contain(e => e.Campo == "maximoParticipantes");
    }

    [Fact]
    public void Grupal_ConCapacidadValida_EsValido()
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = 4;
        dto.MaximoParticipantesPorEquipo = 2;

        Validar(dto).EsValido.Should().BeTrue();
    }

    [Fact]
    public void Grupal_SinMaximoEquipos_Falla()
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = null;
        dto.MaximoParticipantesPorEquipo = 2;

        Validar(dto).Errores.Should().Contain(e => e.Campo == "maximoEquipos");
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(5, 1)]
    [InlineData(5, 0)]
    public void Grupal_PorDebajoDelMinimo_Falla(int equipos, int porEquipo)
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = equipos;
        dto.MaximoParticipantesPorEquipo = porEquipo;

        Validar(dto).EsValido.Should().BeFalse();
    }

    [Fact]
    public void ModoInvalido_Falla()
    {
        var dto = Base("Hibrida");
        dto.MaximoParticipantes = 10;

        Validar(dto).Errores.Should().Contain(e => e.Campo == "modo");
    }

    [Fact]
    public void MisionesDuplicadas_Falla()
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = 10;
        var repetida = Guid.NewGuid();
        dto.MisionesIds = new List<Guid> { repetida, repetida };

        Validar(dto).Errores.Should().Contain(e => e.Campo == "misionesIds");
    }

    [Fact]
    public void SinMisiones_Falla()
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = 10;
        dto.MisionesIds = new List<Guid>();

        Validar(dto).Errores.Should().Contain(e => e.Campo == "misionesIds");
    }
}
