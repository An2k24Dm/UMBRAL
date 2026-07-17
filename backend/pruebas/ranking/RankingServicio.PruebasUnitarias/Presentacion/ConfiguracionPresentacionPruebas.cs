using System.Text.Json;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using Moq;
using RankingServicio.Presentacion.Configuraciones;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Presentacion;

public class ConfiguracionPresentacionPruebas
{
    [Fact]
    public void RespuestaErrorModelo_ModelState_ConstruyeBadRequestConErrores()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("top", "Debe ser mayor a cero.");
        modelState.AddModelError("orden", "");

        var resultado = RespuestaErrorModelo.ConstruirDesdeModelState(modelState);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        var json = Serializar(resultado.Value);
        json.Should().Contain("VALIDACION");
        json.Should().Contain("Debe ser mayor a cero.");
        json.Should().Contain("Valor inválido.");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Digest abc", null)]
    [InlineData("Bearer ranking-token", "ranking-token")]
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
