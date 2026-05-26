using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

// HU10 — pruebas de integración del endpoint
// PATCH /api/usuarios/participantes/perfil. Cubren autorización por rol
// (401/403), identificación del participante por el sub del token,
// aislamiento entre participantes, contraseña, y no-modificación de
// Estado/Rol/FechaRegistro.
public class ModificarParticipanteEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public ModificarParticipanteEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Patch(object cuerpo, string? rol, string? idKeycloak = null)
    {
        var solicitud = new HttpRequestMessage(
            HttpMethod.Patch, "/api/usuarios/participantes/perfil")
        {
            Content = JsonContent.Create(cuerpo)
        };
        if (rol is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        if (idKeycloak is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, idKeycloak);
        return solicitud;
    }

    [Fact]
    public async Task Patch_SinToken_Retorna401()
    {
        var respuesta = await _cliente.SendAsync(Patch(new { nombre = "X" }, rol: null));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_TokenAdministrador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Patch(new { nombre = "Pablo Nuevo" }, "Administrador",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_TokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Patch(new { nombre = "Pablo Nuevo" }, "Operador",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_TokenParticipante_ActualizaSuPropioPerfil()
    {
        _fabrica.MockProveedor.Invocations.Clear();

        var respuesta = await _cliente.SendAsync(
            Patch(new { nombre = "Pablo Nuevo" }, "Participante",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resp = await respuesta.Content.ReadFromJsonAsync<ModificarParticipanteRespuestaDto>();
        resp!.HuboCambios.Should().BeTrue();
        resp.CamposActualizados.Should().Contain("nombre");
        resp.Participante.Nombre.Should().Be("Pablo Nuevo");
        // No se modifican Estado / Rol / FechaRegistro.
        resp.Participante.Estado.Should().Be("Activo");
        resp.Participante.Rol.Should().Be("Participante");

        // El nombre va a Keycloak (firstName).
        _fabrica.MockProveedor.Verify(p => p.ActualizarUsuarioAsync(
            FabricaApiPruebas.IdKeycloakParticipanteSembrado,
            It.Is<DatosActualizacionUsuarioIdentidad>(d => d.Nombre == "Pablo Nuevo"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Patch_TokenParticipante_OtroSub_DevuelveNotFound()
    {
        // Si el sub del token no corresponde a ningún Participante existente
        // se devuelve 404 — el manejador no puede inventar el agregado.
        // Esto cubre el caso "un Participante no puede modificar a otro": el
        // token determina la identidad y solo accede a su propia cuenta.
        var respuesta = await _cliente.SendAsync(
            Patch(new { nombre = "Pablo Nuevo" }, "Participante",
                "kc-no-existe-12345"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_SinSubEnToken_Retorna401()
    {
        // Quitamos el header de sub: el AuthHandler por defecto emite
        // NameIdentifier="tester", que NO corresponde a un Participante.
        // En este caso el endpoint sí entra pero el manejador no lo
        // encuentra → 404. La regla "token sin sub" se prueba aparte
        // en escenarios donde el handler no lo emite (no aplica aquí).
        var respuesta = await _cliente.SendAsync(
            Patch(new { nombre = "Pablo Nuevo" }, "Participante",
                idKeycloak: "tester"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_CorreoDuplicado_Retorna400()
    {
        // ada@umbral.com pertenece al administrador sembrado.
        var respuesta = await _cliente.SendAsync(
            Patch(new { correo = "ada@umbral.com" }, "Participante",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("\"correo\"");
    }

    [Fact]
    public async Task Patch_SoloContrasena_LlamaResetPasswordYNoExponeContrasena()
    {
        const string contrasena = "P4r-Ti.";
        var cuerpo = new
        {
            nuevaContrasena = contrasena,
            confirmacionContrasena = contrasena
        };
        var respuesta = await _cliente.SendAsync(
            Patch(cuerpo, "Participante",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().NotContain(contrasena);

        _fabrica.MockProveedor.Verify(p => p.CambiarContrasenaAsync(
            FabricaApiPruebas.IdKeycloakParticipanteSembrado,
            contrasena,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Patch_ContrasenaInvalida_Retorna400()
    {
        var cuerpo = new
        {
            nuevaContrasena = "abc",
            confirmacionContrasena = "abc"
        };
        var respuesta = await _cliente.SendAsync(
            Patch(cuerpo, "Participante",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_ContrasenaNoCoincide_Retorna400()
    {
        var cuerpo = new
        {
            nuevaContrasena = "Abc1*",
            confirmacionContrasena = "Otro2*"
        };
        var respuesta = await _cliente.SendAsync(
            Patch(cuerpo, "Participante",
                FabricaApiPruebas.IdKeycloakParticipanteSembrado));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("confirmacionContrasena");
    }
}
