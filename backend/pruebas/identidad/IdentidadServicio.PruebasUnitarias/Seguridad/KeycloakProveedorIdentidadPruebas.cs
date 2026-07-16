using System.Net;
using System.Text;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Infraestructura.Seguridad;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentidadServicio.PruebasUnitarias.Seguridad;

public class KeycloakProveedorIdentidadPruebas
{
    [Fact]
    public async Task IniciarSesionAsync_CredencialesValidasDevuelveTokenYSubDelJwt()
    {
        var manejador = new ManejadorHttpFalso((solicitud, _) =>
            Json(HttpStatusCode.OK,
                "{\"access_token\":\"" + JwtConSub("kc-123") +
                "\",\"refresh_token\":\"refresh-1\",\"expires_in\":90,\"token_type\":\"Bearer\"}"));
        var proveedor = CrearProveedor(manejador);

        var resultado = await proveedor.IniciarSesionAsync(
            "operador01", "Clave1!", CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.IdKeycloak.Should().Be("kc-123");
        resultado.TokenAcceso.Should().Contain(".");
        resultado.TokenRefresco.Should().Be("refresh-1");
        resultado.ExpiraEnSegundos.Should().Be(90);
        resultado.TipoToken.Should().Be("Bearer");
        manejador.Solicitudes.Should().ContainSingle();
        manejador.Solicitudes[0].Cuerpo.Should().Contain("grant_type=password");
        manejador.Solicitudes[0].Cuerpo.Should().Contain("username=operador01");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task IniciarSesionAsync_CredencialesRechazadasDevuelveNull(HttpStatusCode estado)
    {
        var manejador = new ManejadorHttpFalso((_, _) =>
            Json(estado, "{\"error\":\"invalid_grant\"}"));
        var proveedor = CrearProveedor(manejador);

        var resultado = await proveedor.IniciarSesionAsync(
            "operador01", "mala", CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task CrearUsuarioAsync_EnviaBearerAdminYDevuelveIdDesdeLocation()
    {
        var manejador = new ManejadorHttpFalso((solicitud, _) =>
        {
            if (solicitud.RequestUri!.AbsoluteUri.EndsWith("/protocol/openid-connect/token"))
                return Json(HttpStatusCode.OK, "{\"access_token\":\"admin-token\"}");

            var respuesta = Json(HttpStatusCode.Created, "{}");
            respuesta.Headers.Location = new Uri(
                "https://login.umbral.local/admin/realms/umbral/users/kc-nuevo");
            return respuesta;
        });
        var proveedor = CrearProveedor(manejador);

        var id = await proveedor.CrearUsuarioAsync(
            new DatosCreacionUsuarioIdentidad(
                "operador01",
                "op@umbral.local",
                "Olivia",
                "Operadora",
                "Clave1!"),
            CancellationToken.None);

        id.Should().Be("kc-nuevo");
        manejador.Solicitudes.Should().HaveCount(2);
        var creacion = manejador.Solicitudes[1];
        creacion.Solicitud.Headers.Authorization!.Scheme.Should().Be("Bearer");
        creacion.Solicitud.Headers.Authorization!.Parameter.Should().Be("admin-token");
        creacion.Cuerpo.Should().Contain("\"username\":\"operador01\"");
        creacion.Cuerpo.Should().Contain("\"temporary\":false");
    }

    [Fact]
    public async Task CrearUsuarioAsync_SinLocationLanzaErrorControlado()
    {
        var manejador = new ManejadorHttpFalso((solicitud, _) =>
            solicitud.RequestUri!.AbsoluteUri.EndsWith("/protocol/openid-connect/token")
                ? Json(HttpStatusCode.OK, "{\"access_token\":\"admin-token\"}")
                : Json(HttpStatusCode.Created, "{}"));
        var proveedor = CrearProveedor(manejador);

        var accion = () => proveedor.CrearUsuarioAsync(
            new DatosCreacionUsuarioIdentidad(
                "operador01",
                "op@umbral.local",
                "Olivia",
                "Operadora",
                "Clave1!"),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Location*");
    }

    [Fact]
    public async Task EliminarUsuarioAsync_NotFoundSeConsideraIdempotente()
    {
        var manejador = new ManejadorHttpFalso((solicitud, _) =>
            solicitud.RequestUri!.AbsoluteUri.EndsWith("/protocol/openid-connect/token")
                ? Json(HttpStatusCode.OK, "{\"access_token\":\"admin-token\"}")
                : Json(HttpStatusCode.NotFound, "{}"));
        var proveedor = CrearProveedor(manejador);

        await proveedor.EliminarUsuarioAsync("kc-inexistente", CancellationToken.None);

        manejador.Solicitudes.Should().HaveCount(2);
        manejador.Solicitudes[1].Solicitud.Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task CambiarContrasenaAsync_ValidaArgumentosObligatorios()
    {
        var proveedor = CrearProveedor(new ManejadorHttpFalso((_, _) =>
            Json(HttpStatusCode.OK, "{}")));

        await proveedor.Invoking(p => p.CambiarContrasenaAsync(
                "", "Clave1!", CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("idKeycloak");
        await proveedor.Invoking(p => p.CambiarContrasenaAsync(
                "kc-1", "", CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("nuevaContrasena");
    }

    [Fact]
    public void OpcionesKeycloak_ConstruyeUrlsDesdeAuthority()
    {
        var opciones = Opciones();

        opciones.UrlBase.Should().Be("https://login.umbral.local");
        opciones.Realm.Should().Be("umbral");
        opciones.UrlToken.Should()
            .Be("https://login.umbral.local/realms/umbral/protocol/openid-connect/token");
        opciones.UrlJwks.Should()
            .Be("https://login.umbral.local/realms/umbral/protocol/openid-connect/certs");
        opciones.MetadataAddress.Should()
            .Be("https://login.umbral.local/realms/umbral/.well-known/openid-configuration");
        opciones.UrlTokenAdmin.Should()
            .Be("https://login.umbral.local/realms/master/protocol/openid-connect/token");
        opciones.UrlAdminUsuarios.Should()
            .Be("https://login.umbral.local/admin/realms/umbral/users");
        opciones.UrlAdminUsuario("kc-1").Should()
            .Be("https://login.umbral.local/admin/realms/umbral/users/kc-1");
        opciones.UrlAdminRol("Operador").Should()
            .Be("https://login.umbral.local/admin/realms/umbral/roles/Operador");
        opciones.UrlAdminAsignarRol("kc-1").Should()
            .Be("https://login.umbral.local/admin/realms/umbral/users/kc-1/role-mappings/realm");
        opciones.UrlAdminCambiarContrasena("kc-1").Should()
            .Be("https://login.umbral.local/admin/realms/umbral/users/kc-1/reset-password");
    }

    [Fact]
    public void OpcionesKeycloak_SinAuthorityDevuelveBaseYRealmVacios()
    {
        var opciones = new OpcionesKeycloak();

        opciones.UrlBase.Should().BeEmpty();
        opciones.Realm.Should().BeEmpty();
    }

    private static KeycloakProveedorIdentidad CrearProveedor(ManejadorHttpFalso manejador) =>
        new(new HttpClient(manejador),
            Options.Create(Opciones()),
            NullLogger<KeycloakProveedorIdentidad>.Instance);

    private static OpcionesKeycloak Opciones() => new()
    {
        Authority = "https://login.umbral.local/realms/umbral/",
        ClientId = "umbral-api",
        ClientSecret = "secreto",
        AdminUsuario = "admin",
        AdminContrasena = "admin123"
    };

    private static string JwtConSub(string sub)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"" + sub + "\"}"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "encabezado." + payload + ".firma";
    }

    private static HttpResponseMessage Json(HttpStatusCode estado, string json) =>
        new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class ManejadorHttpFalso : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

        public List<(HttpRequestMessage Solicitud, string? Cuerpo)> Solicitudes { get; } = new();

        public ManejadorHttpFalso(
            Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
            _responder = responder;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Solicitudes.Add((request, cuerpo));
            return _responder(request, cancellationToken);
        }
    }
}
