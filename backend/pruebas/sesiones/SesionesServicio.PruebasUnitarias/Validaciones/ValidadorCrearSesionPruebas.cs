using System;
using System.Collections.Generic;
using System.Linq;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.PruebasUnitarias.Validaciones;

// Reglas de capacidad del DTO de creación. La capacidad real la define el
// operador por sesión; el validador solo comprueba presencia y rangos.
public class ValidadorCrearSesionPruebas
{
    private static readonly DateTime Fecha = new(2026, 6, 3, 13, 0, 0, DateTimeKind.Utc);
    private readonly ValidadorCrearSesion _validador = new();

    private static CrearSesionSolicitudDto Base(string modo) => new()
    {
        Nombre = "Sesión piloto",
        Descripcion = "Demo",
        Modo = modo,
        FechaProgramada = Fecha,
        MisionesIds = new List<Guid> { Guid.NewGuid() }
    };

    private ResultadoValidacion Validar(CrearSesionSolicitudDto dto)
        => _validador.Validar(new CrearSesionComando(dto));

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

        var resultado = Validar(dto);

        resultado.EsValido.Should().BeFalse();
        resultado.Errores.Should().Contain(e => e.Campo == "maximoParticipantes");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Individual_CapacidadNoPositiva_Falla(int maximo)
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = maximo;

        Validar(dto).Errores.Should().Contain(e => e.Campo == "maximoParticipantes");
    }

    [Fact]
    public void Individual_ValorAlto_EsValido_SinTopeArbitrario()
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = 1_000_000;

        Validar(dto).EsValido.Should().BeTrue();
    }

    [Fact]
    public void Individual_IgnoraCamposGrupales()
    {
        var dto = Base("Individual");
        dto.MaximoParticipantes = 10;
        dto.MaximoEquipos = 999;            // se ignora
        dto.MaximoParticipantesPorEquipo = -3; // se ignora

        Validar(dto).EsValido.Should().BeTrue();
    }

    [Fact]
    public void Grupal_ConCapacidadValida_EsValido()
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = 5;
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

    [Fact]
    public void Grupal_SinMaximoParticipantesPorEquipo_Falla()
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = 5;
        dto.MaximoParticipantesPorEquipo = null;

        Validar(dto).Errores.Should().Contain(e => e.Campo == "maximoParticipantesPorEquipo");
    }

    [Theory]
    [InlineData(0, 2)]   // equipos por debajo del mínimo (1)
    [InlineData(5, 0)]
    [InlineData(5, 1)]   // participantes por equipo por debajo del mínimo (2)
    [InlineData(-1, -1)]
    public void Grupal_PorDebajoDelMinimo_Falla(int equipos, int porEquipo)
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = equipos;
        dto.MaximoParticipantesPorEquipo = porEquipo;

        Validar(dto).EsValido.Should().BeFalse();
    }

    [Fact]
    public void Grupal_IgnoraMaximoParticipantes()
    {
        var dto = Base("Grupal");
        dto.MaximoEquipos = 5;
        dto.MaximoParticipantesPorEquipo = 2;
        dto.MaximoParticipantes = -10; // se ignora en modo grupal

        Validar(dto).EsValido.Should().BeTrue();
    }
}
