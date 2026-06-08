using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

public class AutenticacionEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public AutenticacionEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private void TokenValido(string idKc) =>
        _fabrica.MockProveedor
            .Setup(p => p.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoAutenticacionExterna("acc", "ref", 300, "Bearer", idKc));

    [Fact]
    public async Task LoginWeb_Administrador_Retorna200()
    {
        TokenValido("kc-admin");

        var respuesta = await _cliente.PostAsJsonAsync("/api/autenticacion/login-web",
            new InicioSesionDto { NombreUsuario = "admin_umbral", Contrasena = "Temporal123*" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResultadoInicioSesionDto>();
        cuerpo!.RutaRedireccion.Should().Be("/administrador");
    }

    [Fact]
    public async Task LoginWeb_ParticipanteActivo_Retorna403()
    {
        TokenValido("kc-par-activo");

        var respuesta = await _cliente.PostAsJsonAsync("/api/autenticacion/login-web",
            new InicioSesionDto { NombreUsuario = "participante01", Contrasena = "x" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LoginMovil_ParticipanteActivo_Retorna200()
    {
        TokenValido("kc-par-activo");

        var respuesta = await _cliente.PostAsJsonAsync("/api/autenticacion/login-movil",
            new InicioSesionDto { NombreUsuario = "participante01", Contrasena = "x" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResultadoInicioSesionDto>();
        cuerpo!.RutaRedireccion.Should().Be("/participante/sesiones");
    }

    [Fact]
    public async Task LoginMovil_Administrador_Retorna403()
    {
        TokenValido("kc-admin");

        var respuesta = await _cliente.PostAsJsonAsync("/api/autenticacion/login-movil",
            new InicioSesionDto { NombreUsuario = "admin_umbral", Contrasena = "Temporal123*" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LoginWeb_CuentaInactiva_Retorna403()
    {
        TokenValido("kc-inactivo");

        var respuesta = await _cliente.PostAsJsonAsync("/api/autenticacion/login-web",
            new InicioSesionDto { NombreUsuario = "inactivo01", Contrasena = "x" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPerfilActual_SinToken_Retorna401()
    {
        var respuesta = await _cliente.GetAsync("/api/autenticacion/perfil-actual");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
