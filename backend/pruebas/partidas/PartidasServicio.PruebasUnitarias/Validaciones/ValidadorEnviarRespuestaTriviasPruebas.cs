using PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using PartidasServicio.Aplicacion.Validaciones;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.PruebasUnitarias.Validaciones;

public class ValidadorEnviarRespuestaTriviasPruebas
{
    private static readonly Guid SesionId = Guid.NewGuid();
    private static readonly Guid MisionId = Guid.NewGuid();
    private static readonly Guid EtapaId = Guid.NewGuid();
    private static readonly Guid TriviaId = Guid.NewGuid();
    private static readonly Guid PreguntaId = Guid.NewGuid();
    private static readonly Guid OpcionId = Guid.NewGuid();

    private static EnviarRespuestaTriviaComando ComandoValido() => new(
        SesionId, MisionId, EtapaId, TriviaId,
        new EnviarRespuestaTriviaDto
        {
            PreguntaId = PreguntaId,
            OpcionSeleccionadaId = OpcionId,
            TiempoTardadoMs = 3_000
        });

    private static ValidadorEnviarRespuestaTrivia CrearValidador() => new();

    [Fact]
    public void Validar_ComandoValido_EsExitoso()
    {
        var resultado = CrearValidador().Validar(ComandoValido());

        resultado.EsExitoso.Should().BeTrue();
        resultado.Errores.Should().BeEmpty();
    }

    [Fact]
    public void Validar_SesionIdVacio_AgregaError()
    {
        var comando = ComandoValido() with { SesionId = Guid.Empty };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().ContainSingle(e => e.Campo == "sesionId");
    }

    [Fact]
    public void Validar_MisionIdVacio_AgregaError()
    {
        var comando = ComandoValido() with { MisionId = Guid.Empty };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().ContainSingle(e => e.Campo == "misionId");
    }

    [Fact]
    public void Validar_EtapaIdVacio_AgregaError()
    {
        var comando = ComandoValido() with { EtapaId = Guid.Empty };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().ContainSingle(e => e.Campo == "etapaId");
    }

    [Fact]
    public void Validar_PreguntaIdVacio_AgregaError()
    {
        var dto = new EnviarRespuestaTriviaDto
        {
            PreguntaId = Guid.Empty,
            OpcionSeleccionadaId = OpcionId,
            TiempoTardadoMs = 1_000
        };
        var comando = ComandoValido() with { Dto = dto };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().ContainSingle(e => e.Campo == "preguntaId");
    }

    [Fact]
    public void Validar_OpcionIdVacio_AgregaError()
    {
        var dto = new EnviarRespuestaTriviaDto
        {
            PreguntaId = PreguntaId,
            OpcionSeleccionadaId = Guid.Empty,
            TiempoTardadoMs = 1_000
        };
        var comando = ComandoValido() with { Dto = dto };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().ContainSingle(e => e.Campo == "opcionSeleccionadaId");
    }

    [Fact]
    public void Validar_TiempoNegativo_AgregaError()
    {
        var dto = new EnviarRespuestaTriviaDto
        {
            PreguntaId = PreguntaId,
            OpcionSeleccionadaId = OpcionId,
            TiempoTardadoMs = -1
        };
        var comando = ComandoValido() with { Dto = dto };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().ContainSingle(e => e.Campo == "tiempoTardadoMs");
    }

    [Fact]
    public void Validar_TiempoCero_EsValido()
    {
        var dto = new EnviarRespuestaTriviaDto
        {
            PreguntaId = PreguntaId,
            OpcionSeleccionadaId = OpcionId,
            TiempoTardadoMs = 0
        };
        var comando = ComandoValido() with { Dto = dto };

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeTrue();
    }

    [Fact]
    public void Validar_MultiplesErrores_LosReportaTodos()
    {
        var dto = new EnviarRespuestaTriviaDto
        {
            PreguntaId = Guid.Empty,
            OpcionSeleccionadaId = Guid.Empty,
            TiempoTardadoMs = -5
        };
        var comando = new EnviarRespuestaTriviaComando(
            Guid.Empty, Guid.Empty, Guid.Empty, TriviaId, dto);

        var resultado = CrearValidador().Validar(comando);

        resultado.EsExitoso.Should().BeFalse();
        resultado.Errores.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void LanzarSiHayErrores_ConErrores_LanzaExcepcionValidacion()
    {
        var dto = new EnviarRespuestaTriviaDto
        {
            PreguntaId = Guid.Empty,
            OpcionSeleccionadaId = OpcionId,
            TiempoTardadoMs = 0
        };
        var comando = ComandoValido() with { Dto = dto };

        var accion = () => CrearValidador().Validar(comando).LanzarSiHayErrores();

        accion.Should().Throw<ExcepcionValidacion>();
    }
}
