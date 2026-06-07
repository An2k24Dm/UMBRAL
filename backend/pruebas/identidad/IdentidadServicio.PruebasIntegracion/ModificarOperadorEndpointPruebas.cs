using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

// HU09 — pruebas de integración del endpoint
// PATCH /api/usuarios/operadores/{id}. Cubren autorización por rol (401/403),
// rechazo de id no-Operador (Administrador/Participante), preservación de
// Estado y FechaRegistro, rechazo de correo duplicado y consistencia del
// listado posterior a la edición.
public class ModificarOperadorEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public ModificarOperadorEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Patch(
        Guid id, object cuerpo, string? rol)
    {
        var solicitud = new HttpRequestMessage(
            HttpMethod.Patch, $"/api/usuarios/operadores/{id}")
        {
            Content = JsonContent.Create(cuerpo)
        };
        if (rol is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        return solicitud;
    }

    [Fact]
    public async Task Patch_SinToken_Retorna401()
    {
        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, new { nombre = "X" }, rol: null));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_TokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, new { nombre = "X" }, "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_TokenParticipante_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, new { nombre = "X" }, "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_TokenAdministrador_SoloNombre_Retorna200_YActualiza()
    {
        _fabrica.MockProveedor.Invocations.Clear();

        var cuerpo = new { nombre = "Olivia Nueva" };
        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, cuerpo, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resp = await respuesta.Content.ReadFromJsonAsync<ModificarOperadorRespuestaDto>();
        resp!.HuboCambios.Should().BeTrue();
        resp.CamposActualizados.Should().Contain("nombre");
        resp.Operador.Nombre.Should().Be("Olivia Nueva");
        resp.Operador.Estado.Should().Be("Activo");
        resp.Operador.Rol.Should().Be("Operador");

        // Cambia "nombre" → Keycloak debe recibir firstName.
        _fabrica.MockProveedor.Verify(p => p.ActualizarUsuarioAsync(
            "kc-op-hu09",
            It.Is<DatosActualizacionUsuarioIdentidad>(d => d.Nombre == "Olivia Nueva"),
            It.IsAny<CancellationToken>()), Times.Once);

        // El listado interno reflejará el cambio en GET.
        var listadoSolicitud = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios/internos?rol=Operador");
        listadoSolicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        var listadoRespuesta = await _cliente.SendAsync(listadoSolicitud);
        listadoRespuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await listadoRespuesta.Content.ReadAsStringAsync();
        json.Should().Contain("Olivia Nueva");
    }

    [Fact]
    public async Task Patch_SobreParticipante_Retorna404()
    {
        // Reutilizamos un id de Participante sembrado: ConsultarParticipantes
        // expone la lista, pero acá basta con confirmar que el manejador
        // rechaza ids que no corresponden a un Operador (devuelve 404).
        var solicitudListado = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios/participantes");
        solicitudListado.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        var respuestaListado = await _cliente.SendAsync(solicitudListado);
        var jsonListado = await respuestaListado.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonListado);
        var idParticipante = doc.RootElement
            .GetProperty("elementos").EnumerateArray().First()
            .GetProperty("id").GetGuid();

        var respuesta = await _cliente.SendAsync(
            Patch(idParticipante, new { nombre = "Pepe" }, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_SobreAdministrador_Retorna404()
    {
        // Busca el administrador sembrado vía el listado interno.
        var solicitudListado = new HttpRequestMessage(
            HttpMethod.Get, "/api/usuarios/internos?rol=Administrador");
        solicitudListado.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        var respuestaListado = await _cliente.SendAsync(solicitudListado);
        var jsonListado = await respuestaListado.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonListado);
        var idAdmin = doc.RootElement
            .GetProperty("elementos").EnumerateArray().First()
            .GetProperty("id").GetGuid();

        var respuesta = await _cliente.SendAsync(
            Patch(idAdmin, new { nombre = "Ada Nueva" }, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_IntentaCambiarEstado_NoModificaEstado()
    {
        // El DTO no expone Estado, pero el cliente puede enviar un campo extra
        // en el JSON. Debe ignorarse: la respuesta sigue siendo Activo.
        var cuerpoConExtras = new
        {
            nombre = "Cambio Defensivo",
            estado = "Inactivo",     // ignorado
            fechaRegistro = "1970-01-01T00:00:00Z", // ignorado
            rol = "Administrador"     // ignorado
        };

        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, cuerpoConExtras, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resp = await respuesta.Content.ReadFromJsonAsync<ModificarOperadorRespuestaDto>();
        resp!.Operador.Estado.Should().Be("Activo");
        resp.Operador.Rol.Should().Be("Operador");
    }

    [Fact]
    public async Task Patch_CorreoDuplicado_Retorna400()
    {
        // ada@umbral.com pertenece al administrador sembrado.
        var cuerpo = new { correo = "ada@umbral.com" };
        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, cuerpo, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("\"correo\"");
    }

    [Fact]
    public async Task Patch_SinCambios_Retorna200_ConHuboCambiosFalse()
    {
        // 1) Garantizar un nombre conocido (puede haber sido cambiado por otra prueba).
        await _cliente.SendAsync(Patch(
            FabricaApiPruebas.IdOperadorSembrado,
            new { nombre = "Olivia" },
            "Administrador"));

        // 2) Volver a enviar el mismo valor: no debe persistir nada.
        var respuesta = await _cliente.SendAsync(
            Patch(FabricaApiPruebas.IdOperadorSembrado, new { nombre = "Olivia" }, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resp = await respuesta.Content.ReadFromJsonAsync<ModificarOperadorRespuestaDto>();
        resp!.HuboCambios.Should().BeFalse();
        resp.CamposActualizados.Should().BeEmpty();
    }

    // Nota: el endpoint de modificación administrativa de Operador ya NO
    // acepta cambio de contraseña. El reseteo se hace con el endpoint
    // dedicado /api/usuarios/internos/{id}/resetear-contrasena.

    // HU09 — si Keycloak falla, la base de datos NO debe quedar modificada.
    // Para que la prueba sea independiente del orden de ejecución se restaura
    // el nombre conocido al inicio y se compara contra ese valor tras forzar
    // el fallo del proveedor de identidad.
    [Fact]
    public async Task Patch_KeycloakFalla_NoModificaBaseDatos()
    {
        // Normalizamos el nombre actual a un valor conocido.
        const string nombreConocido = "OliviaBase";
        await _cliente.SendAsync(Patch(
            FabricaApiPruebas.IdOperadorSembrado,
            new { nombre = nombreConocido },
            "Administrador"));

        // Configuramos el proveedor para que rechace la actualización en Keycloak.
        _fabrica.MockProveedor
            .Setup(p => p.ActualizarUsuarioAsync(
                It.IsAny<string>(),
                It.IsAny<DatosActualizacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak rechazó la actualización"));

        try
        {
            var respuesta = await _cliente.SendAsync(Patch(
                FabricaApiPruebas.IdOperadorSembrado,
                new { nombre = "NoDeberiaPersistir" },
                "Administrador"));

            // El error se propaga al middleware → 500 ERROR_INTERNO.
            respuesta.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            // El nombre en base sigue siendo el conocido: no se persistió el cambio.
            var solicitudDetalle = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/usuarios/internos/{FabricaApiPruebas.IdOperadorSembrado}");
            solicitudDetalle.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
            var detalle = await _cliente.SendAsync(solicitudDetalle);
            detalle.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await detalle.Content.ReadAsStringAsync();
            json.Should().Contain(nombreConocido);
            json.Should().NotContain("NoDeberiaPersistir");
        }
        finally
        {
            // Restablecer el mock para no contaminar otras pruebas que comparten la fábrica.
            _fabrica.MockProveedor.Reset();
        }
    }
}
