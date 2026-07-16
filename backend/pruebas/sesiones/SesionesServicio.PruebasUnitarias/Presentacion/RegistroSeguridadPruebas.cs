using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SesionesServicio.Presentacion.Configuraciones;

namespace SesionesServicio.PruebasUnitarias.Presentacion;

public class RegistroSeguridadPruebas
{
    [Fact]
    public void AgregarSeguridad_ConfiguraJwtBearerYPoliticasEsperadas()
    {
        using var proveedor = ConstruirProveedor(metadataAddress:
            "https://login.umbral.local/realms/umbral/.well-known/openid-configuration");

        var opcionesJwt = Jwt(proveedor);
        var opcionesAuth = proveedor.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        opcionesJwt.Authority.Should().Be("https://login.umbral.local/realms/umbral");
        opcionesJwt.RequireHttpsMetadata.Should().BeFalse();
        opcionesJwt.MetadataAddress.Should()
            .Be("https://login.umbral.local/realms/umbral/.well-known/openid-configuration");
        opcionesJwt.TokenValidationParameters.ValidIssuer.Should()
            .Be("https://login.umbral.local/realms/umbral");
        opcionesJwt.TokenValidationParameters.RoleClaimType.Should().Be("roles");
        opcionesJwt.TokenValidationParameters.NameClaimType.Should().Be("preferred_username");
        opcionesJwt.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(30));
        opcionesAuth.GetPolicy("PoliticaAdministradorUOperador").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaSoloOperador").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaSoloAdministrador").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaSoloParticipante").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaOperadorOParticipante").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaAdministradorOperadorOParticipante").Should().NotBeNull();
    }

    [Fact]
    public void AgregarSeguridad_SiMetadataAddressVieneVacioUsaConvencionDelAuthority()
    {
        using var proveedor = ConstruirProveedor(metadataAddress: "");

        Jwt(proveedor).MetadataAddress.Should()
            .Be("https://login.umbral.local/realms/umbral/.well-known/openid-configuration");
    }

    [Theory]
    [InlineData("/hubs/sesiones", "token-query")]
    [InlineData("/hubs/sesiones/abc", "token-query")]
    [InlineData("/api/sesiones", null)]
    public async Task OnMessageReceived_UsaAccessTokenSoloParaHubSesiones(
        string path,
        string? tokenEsperado)
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = Jwt(proveedor);
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Request.QueryString = new QueryString("?access_token=token-query");
        var contexto = new MessageReceivedContext(http, Esquema(), opcionesJwt);

        await opcionesJwt.Events.OnMessageReceived(contexto);

        contexto.Token.Should().Be(tokenEsperado);
    }

    [Fact]
    public async Task OnMessageReceived_SinAccessTokenNoAsignaToken()
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = Jwt(proveedor);
        var http = new DefaultHttpContext();
        http.Request.Path = "/hubs/sesiones";
        var contexto = new MessageReceivedContext(http, Esquema(), opcionesJwt);

        await opcionesJwt.Events.OnMessageReceived(contexto);

        contexto.Token.Should().BeNull();
    }

    [Fact]
    public async Task OnTokenValidated_AplanaRolesDelRealmAccess()
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = Jwt(proveedor);
        var identidad = new ClaimsIdentity("Bearer");
        identidad.AddClaim(new Claim(
            "realm_access",
            "{\"roles\":[\"Administrador\",\"Participante\",\"\"]}"));
        var principal = new ClaimsPrincipal(identidad);
        var contexto = new TokenValidatedContext(
            new DefaultHttpContext(), Esquema(), opcionesJwt)
        {
            Principal = principal
        };

        await opcionesJwt.Events.OnTokenValidated(contexto);

        principal.FindAll("roles").Select(c => c.Value)
            .Should().BeEquivalentTo("Administrador", "Participante");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task OnTokenValidated_SinRealmAccessNoAgregaRoles(string? realmAccess)
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = Jwt(proveedor);
        var identidad = new ClaimsIdentity("Bearer");
        if (realmAccess is not null)
            identidad.AddClaim(new Claim("realm_access", realmAccess));
        var principal = new ClaimsPrincipal(identidad);
        var contexto = new TokenValidatedContext(
            new DefaultHttpContext(), Esquema(), opcionesJwt)
        {
            Principal = principal
        };

        await opcionesJwt.Events.OnTokenValidated(contexto);

        principal.FindAll("roles").Should().BeEmpty();
    }

    [Fact]
    public async Task OnForbidden_EscribeRespuestaJson403()
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = Jwt(proveedor);
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        var contexto = new ForbiddenContext(http, Esquema(), opcionesJwt);

        await opcionesJwt.Events.OnForbidden(contexto);

        http.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        http.Response.ContentType.Should().Be("application/json");
        var json = await LeerRespuestaAsync(http);
        json.RootElement.GetProperty("codigo").GetString().Should().Be("ACCESO_DENEGADO");
        json.RootElement.GetProperty("mensaje").GetString()
            .Should().Contain("No tienes permisos");
    }

    [Fact]
    public async Task OnChallenge_EscribeRespuestaJson401YMarcaRespuestaComoManejada()
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = Jwt(proveedor);
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        var contexto = new JwtBearerChallengeContext(
            http, Esquema(), opcionesJwt, new AuthenticationProperties());

        await opcionesJwt.Events.OnChallenge(contexto);

        contexto.Handled.Should().BeTrue();
        http.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        http.Response.ContentType.Should().Be("application/json");
        var json = await LeerRespuestaAsync(http);
        json.RootElement.GetProperty("codigo").GetString().Should().Be("NO_AUTENTICADO");
        json.RootElement.GetProperty("mensaje").GetString()
            .Should().Contain("Debe iniciar sesión");
    }

    private static ServiceProvider ConstruirProveedor(string? metadataAddress = null)
    {
        var ajustes = new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://login.umbral.local/realms/umbral"
        };
        if (metadataAddress is not null)
            ajustes["Keycloak:MetadataAddress"] = metadataAddress;

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(ajustes)
            .Build();

        var servicios = new ServiceCollection();
        servicios.AgregarSeguridad(configuracion);
        return servicios.BuildServiceProvider();
    }

    private static JwtBearerOptions Jwt(IServiceProvider proveedor) =>
        proveedor.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

    private static AuthenticationScheme Esquema() =>
        new(JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));

    private static async Task<JsonDocument> LeerRespuestaAsync(HttpContext http)
    {
        http.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(http.Response.Body);
    }
}
