using JuegosServicio.Presentacion.Configuraciones;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace JuegosServicio.PruebasUnitarias.Presentacion;

public class PropagadorTokenActualHttpPruebas
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("ApiKey abc", null)]
    [InlineData("Bearer juegos-token", "juegos-token")]
    [InlineData("bearer   juegos-token-2  ", "juegos-token-2")]
    public void ObtenerTokenActual_ExtraeSoloBearer(string? header, string? esperado)
    {
        var contexto = new DefaultHttpContext();
        if (header is not null)
            contexto.Request.Headers[HeaderNames.Authorization] = header;
        var accesor = Mock.Of<IHttpContextAccessor>(a => a.HttpContext == contexto);
        var propagador = new PropagadorTokenActualHttp(accesor);

        propagador.ObtenerTokenActual().Should().Be(esperado);
    }
}
