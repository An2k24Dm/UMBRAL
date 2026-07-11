using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.ServiciosExternos;

namespace SesionesServicio.PruebasUnitarias.ServiciosExternos;

public sealed class ClienteJuegosMisionesHttpPruebas
{
    private static readonly Guid MisionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EtapaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ModoId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task ObtenerMisionConEtapas_UsaEndpointDeOperadorPrimero()
    {
        var manejador = new ManejadorHttpPrueba(_ => RespuestaJson(HttpStatusCode.OK));
        var cliente = CrearCliente(manejador);

        var resultado = await cliente.ObtenerMisionConEtapasAsync(MisionId, CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Etapas.Should().ContainSingle(e => e.Id == EtapaId);
        manejador.Rutas.Should().Equal($"/api/juegos/misiones/{MisionId}");
        manejador.Autorizaciones.Should().OnlyContain(a => a == "Bearer token-operador");
    }

    [Fact]
    public async Task ObtenerMisionConEtapas_SiEndpointOperadorEsForbidden_IntentaEndpointParticipante()
    {
        var intento = 0;
        var manejador = new ManejadorHttpPrueba(_ =>
        {
            intento++;
            return intento == 1
                ? new HttpResponseMessage(HttpStatusCode.Forbidden)
                : RespuestaJson(HttpStatusCode.OK);
        });
        var cliente = CrearCliente(manejador);

        var resultado = await cliente.ObtenerMisionConEtapasAsync(MisionId, CancellationToken.None);

        resultado.Should().NotBeNull();
        manejador.Rutas.Should().Equal(
            $"/api/juegos/misiones/{MisionId}",
            $"/api/juegos/misiones/participante/{MisionId}");
    }

    private static ClienteJuegosMisionesHttp CrearCliente(ManejadorHttpPrueba manejador)
    {
        var propagador = new Mock<IPropagadorTokenActual>();
        propagador.Setup(p => p.ObtenerTokenActual()).Returns("token-operador");

        return new ClienteJuegosMisionesHttp(
            new HttpClient(manejador),
            Options.Create(new OpcionesJuegosServicio { Url = "http://juegos.local" }),
            propagador.Object);
    }

    private static HttpResponseMessage RespuestaJson(HttpStatusCode estado)
    {
        var json = $$"""
        {
          "id": "{{MisionId}}",
          "nombre": "Mision Alfa",
          "descripcion": "Demo",
          "estado": "Activa",
          "dificultad": "Media",
          "tiempoTotal": 120,
          "etapas": [
            {
              "id": "{{EtapaId}}",
              "orden": 1,
              "tipoModoDeJuego": "Trivia",
              "modoDeJuegoId": "{{ModoId}}",
              "nombreModoDeJuego": "Trivia Alfa",
              "tiempoEstimado": 120
            }
          ]
        }
        """;

        return new HttpResponseMessage(estado)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class ManejadorHttpPrueba : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public List<string> Rutas { get; } = new();
        public List<string?> Autorizaciones { get; } = new();

        public ManejadorHttpPrueba(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Rutas.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            Autorizaciones.Add(request.Headers.Authorization?.ToString());
            return Task.FromResult(_responder(request));
        }
    }
}
