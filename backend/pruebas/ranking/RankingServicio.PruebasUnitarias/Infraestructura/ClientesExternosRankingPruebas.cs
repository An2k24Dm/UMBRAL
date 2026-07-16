using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Infraestructura.ServiciosExternos;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public class ClientesExternosRankingPruebas
{
    [Fact]
    public async Task Identidad_SinUrlConfigurada_DevuelveVacioSinEnviar()
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var cliente = CrearClienteIdentidad(handler, url: "");

        var resultado = await cliente.ObtenerParticipantesPorIdsAsync(
            new[] { Guid.NewGuid() }, CancellationToken.None);

        resultado.Should().BeEmpty();
        handler.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    public async Task Identidad_EnviaIdsDistintosYBearerToken()
    {
        var id = Guid.NewGuid();
        var duplicado = id;
        var handler = new HandlerHttp(_ => JsonResponse(
            $$"""[{"id":"{{id}}","nombre":"Ana","apellido":"Paz","alias":"ana"}]"""));
        var cliente = CrearClienteIdentidad(handler, token: "token-123");

        var resultado = await cliente.ObtenerParticipantesPorIdsAsync(
            new[] { id, Guid.Empty, duplicado }, CancellationToken.None);

        resultado.Should().ContainKey(id);
        resultado[id].Alias.Should().Be("ana");
        var solicitud = handler.Solicitudes.Should().ContainSingle().Subject;
        solicitud.Method.Should().Be(HttpMethod.Post);
        solicitud.RequestUri!.ToString().Should().Be("https://identidad.test/api/usuarios/participantes/por-ids");
        solicitud.Headers.Authorization!.Scheme.Should().Be("Bearer");
        solicitud.Headers.Authorization.Parameter.Should().Be("token-123");
        var cuerpo = handler.Cuerpos.Should().ContainSingle().Subject;
        cuerpo.Should().Contain(id.ToString());
        cuerpo.Should().NotContain(Guid.Empty.ToString());
    }

    [Fact]
    public async Task Identidad_RespuestaNoExitosaOJsonNulo_DevuelveVacio()
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var cliente = CrearClienteIdentidad(handler);

        var resultado = await cliente.ObtenerParticipantesPorIdsAsync(
            new[] { Guid.NewGuid() }, CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Sesiones_SinUrlOIdVacio_DevuelveVacioSinEnviar()
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var cliente = CrearClienteSesiones(handler, url: "");

        var resultado = await cliente.ObtenerNombresEquiposAsync(Guid.NewGuid(), CancellationToken.None);
        var resultadoIdVacio = await CrearClienteSesiones(handler)
            .ObtenerNombresEquiposAsync(Guid.Empty, CancellationToken.None);

        resultado.Should().BeEmpty();
        resultadoIdVacio.Should().BeEmpty();
        handler.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    public async Task Sesiones_EnviaGetConTokenYDeduplicaEquipos()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var handler = new HandlerHttp(_ => JsonResponse(
            "[" +
            $"{{\"id\":\"{equipoId}\",\"nombre\":\"Rojo\"}}," +
            $"{{\"id\":\"{equipoId}\",\"nombre\":\"Duplicado\"}}," +
            "{\"id\":\"00000000-0000-0000-0000-000000000000\",\"nombre\":\"Ignorado\"}" +
            "]"));
        var cliente = CrearClienteSesiones(handler, token: "token-abc");

        var resultado = await cliente.ObtenerNombresEquiposAsync(sesionId, CancellationToken.None);

        resultado.Should().ContainKey(equipoId);
        resultado[equipoId].Should().Be("Rojo");
        var solicitud = handler.Solicitudes.Should().ContainSingle().Subject;
        solicitud.Method.Should().Be(HttpMethod.Get);
        solicitud.RequestUri!.ToString().Should().Be($"https://sesiones.test/api/sesiones/{sesionId}/equipos");
        solicitud.Headers.Authorization!.Parameter.Should().Be("token-abc");
    }

    [Fact]
    public async Task Sesiones_RespuestaNoExitosa_DevuelveVacio()
    {
        var handler = new HandlerHttp(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var cliente = CrearClienteSesiones(handler);

        var resultado = await cliente.ObtenerNombresEquiposAsync(Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    private static ClienteIdentidadParticipantesRanking CrearClienteIdentidad(
        HandlerHttp handler,
        string url = "https://identidad.test",
        string? token = null)
        => new(
            new HttpClient(handler),
            Options.Create(new OpcionesIdentidadServicio { Url = url }),
            Mock.Of<IPropagadorTokenActual>(p => p.ObtenerTokenActual() == token),
            NullLogger<ClienteIdentidadParticipantesRanking>.Instance);

    private static ClienteSesionesRankingHttp CrearClienteSesiones(
        HandlerHttp handler,
        string url = "https://sesiones.test",
        string? token = null)
        => new(
            new HttpClient(handler),
            Options.Create(new OpcionesSesionesServicio { Url = url }),
            Mock.Of<IPropagadorTokenActual>(p => p.ObtenerTokenActual() == token),
            NullLogger<ClienteSesionesRankingHttp>.Instance);

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class HandlerHttp : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<HttpRequestMessage> Solicitudes { get; } = new();
        public List<string> Cuerpos { get; } = new();

        public HandlerHttp(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                Cuerpos.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            Solicitudes.Add(request);
            return _responder(request);
        }
    }
}
