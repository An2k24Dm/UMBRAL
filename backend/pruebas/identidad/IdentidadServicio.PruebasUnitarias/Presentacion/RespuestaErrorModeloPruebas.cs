using System.Text.Json;
using System.Text.Encodings.Web;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Presentacion.Configuraciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IdentidadServicio.PruebasUnitarias.Presentacion;

public class RespuestaErrorModeloPruebas
{
    [Fact]
    public void ExtraerErrores_SinErrores_DevuelveErrorDeCuerpo()
    {
        var errores = RespuestaErrorModelo.ExtraerErrores(new ModelStateDictionary());

        Serializar(errores).Should()
            .Contain(RespuestaErrorModelo.MensajeCuerpoInvalido);
    }

    [Fact]
    public void ConstruirDesdeModelState_FechaNacimiento_UsaMensajeCatalogado()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("$.fechaNacimiento", "mensaje técnico");

        var resultado = RespuestaErrorModelo.ConstruirDesdeModelState(modelState);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        var json = Serializar(((BadRequestObjectResult)resultado).Value);
        json.Should().Contain(MensajesValidacionUsuario.CampoFechaNacimiento);
        json.Should().Contain(MensajesValidacionUsuario.FechaNacimientoFormato);
        json.Should().NotContain("mensaje técnico");
    }

    [Theory]
    [InlineData(null, "cuerpo")]
    [InlineData("$.Nombre", "nombre")]
    [InlineData("$.perfil.Correo", "perfil.correo")]
    public void ConstruirDesdeJsonException_NormalizaCampo(string? path, string campoEsperado)
    {
        var respuesta = RespuestaErrorModelo.ConstruirDesdeJsonException(
            new JsonException("json inválido", path: path, lineNumber: null, bytePositionInLine: null),
            "corr-identidad");

        var json = Serializar(respuesta);
        json.Should().Contain(campoEsperado);
        json.Should().Contain("corr-identidad");
    }

    private static string Serializar(object? valor)
        => JsonSerializer.Serialize(valor, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
}
