using System.Security.Claims;
using IdentidadServicio.Presentacion.Configuraciones;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdentidadServicio.PruebasUnitarias.Presentacion;

public class RegistroSeguridadPruebas
{
    [Fact]
    public void AgregarSeguridad_ConfiguraJwtBearerConKeycloakYPoliticas()
    {
        using var proveedor = ConstruirProveedor();

        var opcionesJwt = proveedor
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var opcionesAuth = proveedor
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>()
            .Value;

        opcionesJwt.Authority.Should().Be("https://login.umbral.local/realms/umbral");
        opcionesJwt.MetadataAddress.Should()
            .Be("https://login.umbral.local/realms/umbral/.well-known/openid-configuration");
        opcionesJwt.RequireHttpsMetadata.Should().BeFalse();
        opcionesJwt.TokenValidationParameters.ValidIssuer.Should()
            .Be("https://login.umbral.local/realms/umbral");
        opcionesJwt.TokenValidationParameters.RoleClaimType.Should().Be("roles");
        opcionesJwt.TokenValidationParameters.NameClaimType.Should().Be("preferred_username");
        opcionesJwt.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(30));
        opcionesAuth.GetPolicy("PoliticaAdministrador").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaOperador").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaParticipante").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaAdministradorUOperador").Should().NotBeNull();
        opcionesAuth.GetPolicy("PoliticaConsultaParticipantesBasicos").Should().NotBeNull();
    }

    [Fact]
    public async Task OnTokenValidated_AplanaRolesDelRealmAccess()
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = proveedor
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var identidad = new ClaimsIdentity("Bearer");
        identidad.AddClaim(new Claim(
            "realm_access",
            "{\"roles\":[\"Administrador\",\"Operador\",\"\"]}"));
        var principal = new ClaimsPrincipal(identidad);
        var esquema = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));
        var contexto = new TokenValidatedContext(new DefaultHttpContext(), esquema, opcionesJwt)
        {
            Principal = principal
        };

        await opcionesJwt.Events.OnTokenValidated(contexto);

        principal.FindAll("roles").Select(c => c.Value)
            .Should().BeEquivalentTo("Administrador", "Operador");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task OnTokenValidated_SinRealmAccessNoAgregaRoles(string? valorRealmAccess)
    {
        using var proveedor = ConstruirProveedor();
        var opcionesJwt = proveedor
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var identidad = new ClaimsIdentity("Bearer");
        if (valorRealmAccess is not null)
            identidad.AddClaim(new Claim("realm_access", valorRealmAccess));
        var principal = new ClaimsPrincipal(identidad);
        var esquema = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));
        var contexto = new TokenValidatedContext(new DefaultHttpContext(), esquema, opcionesJwt)
        {
            Principal = principal
        };

        await opcionesJwt.Events.OnTokenValidated(contexto);

        principal.FindAll("roles").Should().BeEmpty();
    }

    private static ServiceProvider ConstruirProveedor()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://login.umbral.local/realms/umbral"
            })
            .Build();

        var servicios = new ServiceCollection();
        servicios.AgregarSeguridad(configuracion);
        return servicios.BuildServiceProvider();
    }
}
