using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using SesionesServicio.Presentacion.Configuraciones;

namespace SesionesServicio.PruebasUnitarias.Presentacion;

public class ConfiguracionPresentacionPruebas
{
    [Fact]
    public void RespuestaErrorModelo_ModelState_UsaMensajePorDefectoSiEstaVacio()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("nombre", "");

        var resultado = RespuestaErrorModelo.ConstruirDesdeModelState(modelState);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        Serializar(resultado.Value).Should().Contain("Valor inválido.");
    }

    [Fact]
    public void RespuestaErrorModelo_JsonException_UsaSolicitudSiNoHayPath()
    {
        var respuesta = RespuestaErrorModelo.ConstruirDesdeJsonException(
            new JsonException("json inválido"));

        var json = Serializar(respuesta);
        json.Should().Contain("solicitud");
        json.Should().Contain("json inválido");
    }

    [Fact]
    public void RespuestaErrorModelo_JsonException_IncluyeCorrelationId()
    {
        var respuesta = RespuestaErrorModelo.ConstruirDesdeJsonException(
            new JsonException("json inválido", path: "$.fecha", lineNumber: 1, bytePositionInLine: 2),
            "corr-1");

        var json = Serializar(respuesta);
        json.Should().Contain("$.fecha");
        json.Should().Contain("corr-1");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Basic abc", null)]
    [InlineData("Bearer token-123", "token-123")]
    [InlineData("bearer   token-abc  ", "token-abc")]
    public void PropagadorTokenActualHttp_ExtraeSoloBearer(string? header, string? esperado)
    {
        var contexto = new DefaultHttpContext();
        if (header is not null)
            contexto.Request.Headers[HeaderNames.Authorization] = header;
        var accesor = Mock.Of<IHttpContextAccessor>(a => a.HttpContext == contexto);
        var propagador = new PropagadorTokenActualHttp(accesor);

        propagador.ObtenerTokenActual().Should().Be(esperado);
    }

    private static string Serializar(object? valor)
        => JsonSerializer.Serialize(valor, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
}
