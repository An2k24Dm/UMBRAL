using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

public class UsuariosEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public UsuariosEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private static int _telefonoSecuencia = 0;

    private static string TelefonoUnico()
    {
        var idx = Interlocked.Increment(ref _telefonoSecuencia);
        return "0414" + idx.ToString("D7");
    }

    private static CrearUsuarioDto DtoOperador() => new()
    {
        TipoUsuario = RolUsuario.Operador,
        NombreUsuario = "op" + Guid.NewGuid().ToString("N").Substring(0, 8),
        Correo = $"{Guid.NewGuid():N}@umbral.com",
        Nombre = "Olivia",
        Apellido = "Operadora",
        Sexo = "Femenino",
        FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto { Direccion = "Caracas", Telefono = TelefonoUnico() }
    };

    private static CrearUsuarioDto DtoAdministrador()
    {
        var dto = DtoOperador();
        dto.TipoUsuario = RolUsuario.Administrador;
        dto.NombreUsuario = "ad" + Guid.NewGuid().ToString("N").Substring(0, 8);
        return dto;
    }

    private void ConfigurarKeycloakOk(string idKc = "kc-x")
    {
        _fabrica.MockProveedor
            .Setup(p => p.CrearUsuarioAsync(
                It.IsAny<DatosCreacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(idKc);
        _fabrica.MockProveedor
            .Setup(p => p.AsignarRolAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private HttpRequestMessage Solicitud(CrearUsuarioDto dto, string? rol)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(dto)
        };
        if (rol is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        return solicitud;
    }

    private async Task<RespuestaErrorValidacion?> LeerErroresAsync(HttpResponseMessage respuesta)
    {
        var json = await respuesta.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RespuestaErrorValidacion>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    [Fact]
    public async Task PostUsuarios_Operador_SinToken_Retorna401()
    {
        ConfigurarKeycloakOk();
        var respuesta = await _cliente.SendAsync(Solicitud(DtoOperador(), rol: null));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostUsuarios_Operador_TokenParticipante_Retorna403()
    {
        ConfigurarKeycloakOk();
        var respuesta = await _cliente.SendAsync(Solicitud(DtoOperador(), "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostUsuarios_Operador_TokenOperador_Retorna403()
    {
        ConfigurarKeycloakOk();
        var respuesta = await _cliente.SendAsync(Solicitud(DtoOperador(), "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // HU02: registro de Operador con token Administrador → 201 + código generado.
    [Fact]
    public async Task PostUsuarios_Operador_TokenAdministrador_Retorna201_ConCodigoGenerado()
    {
        ConfigurarKeycloakOk($"kc-op-{Guid.NewGuid():N}");

        var respuesta = await _cliente.SendAsync(Solicitud(DtoOperador(), "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CrearUsuarioRespuestaDto>();
        cuerpo!.Rol.Should().Be("Operador");
        cuerpo.Estado.Should().Be("Activo");
        cuerpo.Codigo.Should().NotBeNullOrWhiteSpace();
        cuerpo.Codigo.Should().StartWith("OP-");
        cuerpo.Codigo!.Length.Should().Be(6); // "OP-" + 3 dígitos
    }

    // HU02: registro de Administrador → 201 + código AD-###.
    [Fact]
    public async Task PostUsuarios_Administrador_TokenAdministrador_Retorna201_ConCodigoAD()
    {
        ConfigurarKeycloakOk($"kc-adm-{Guid.NewGuid():N}");

        var respuesta = await _cliente.SendAsync(Solicitud(DtoAdministrador(), "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CrearUsuarioRespuestaDto>();
        cuerpo!.Rol.Should().Be("Administrador");
        cuerpo.Codigo.Should().NotBeNullOrWhiteSpace();
        cuerpo.Codigo.Should().StartWith("AD-");
    }

    // HU02: el frontend no envía codigoOperador; aunque el JSON traiga uno
    // (propiedad extra), debe ignorarse y el backend genera el código.
    [Fact]
    public async Task PostUsuarios_Operador_IgnoraCodigoEnviadoPorCliente()
    {
        ConfigurarKeycloakOk($"kc-op-{Guid.NewGuid():N}");
        var dto = DtoOperador();
        var cuerpoConCodigoExtra = new
        {
            tipoUsuario = "Operador",
            nombreUsuario = dto.NombreUsuario,
            correo = dto.Correo,
            nombre = dto.Nombre,
            apellido = dto.Apellido,
            sexo = dto.Sexo,
            fechaNacimiento = dto.FechaNacimiento,
            datosContacto = new { dto.DatosContacto.Direccion, dto.DatosContacto.Telefono },
            codigoOperador = "OP-MANUAL" // será ignorado por el backend.
        };

        var solicitud = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(cuerpoConCodigoExtra)
        };
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        var respuesta = await _cliente.SendAsync(solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var resp = await respuesta.Content.ReadFromJsonAsync<CrearUsuarioRespuestaDto>();
        resp!.Codigo.Should().NotBe("OP-MANUAL");
        resp.Codigo.Should().StartWith("OP-");
    }

    [Fact]
    public async Task PostUsuarios_Operador_CorreoDuplicado_Retorna400_ConErrorPorCampo()
    {
        ConfigurarKeycloakOk();

        var dto = DtoOperador();
        dto.Correo = "ada@umbral.com";

        var respuesta = await _cliente.SendAsync(Solicitud(dto, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerErroresAsync(respuesta))!.Errores.Any(e => e.Campo == "correo")
            .Should().BeTrue();
    }

    // El endpoint de creación ya no recibe contraseña: la genera el backend.
    // Por eso no hay prueba de validación de "contraseña inválida".

    [Fact]
    public async Task PostUsuarios_Operador_TelefonoInvalido_Retorna400_ConErrorPorCampo()
    {
        ConfigurarKeycloakOk();

        var dto = DtoOperador();
        dto.DatosContacto = new DatosContactoDto { Direccion = "Caracas", Telefono = "03123710260" };

        var respuesta = await _cliente.SendAsync(Solicitud(dto, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerErroresAsync(respuesta))!.Errores.Any(e => e.Campo == "datosContacto.telefono")
            .Should().BeTrue();
    }

    [Fact]
    public async Task PostUsuarios_Operador_DireccionVacia_Retorna400_ConErrorPorCampo()
    {
        ConfigurarKeycloakOk();
        _fabrica.MockProveedor.Invocations.Clear();

        var dto = DtoOperador();
        dto.DatosContacto = new DatosContactoDto { Direccion = "", Telefono = TelefonoUnico() };

        var respuesta = await _cliente.SendAsync(Solicitud(dto, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var cuerpo = await LeerErroresAsync(respuesta);
        cuerpo!.Codigo.Should().Be("VALIDACION");
        cuerpo.Errores.Should().Contain(e =>
            e.Campo == "datosContacto.direccion" &&
            e.Mensaje == "La dirección es obligatoria.");

        // Si la dirección está vacía no debe llamarse a Keycloak ni a Guardar*.
        _fabrica.MockProveedor.Verify(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostUsuarios_Operador_DireccionCorta_Retorna400_ConErrorPorCampo()
    {
        ConfigurarKeycloakOk();

        var dto = DtoOperador();
        dto.DatosContacto = new DatosContactoDto { Direccion = "ABC", Telefono = TelefonoUnico() };

        var respuesta = await _cliente.SendAsync(Solicitud(dto, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await LeerErroresAsync(respuesta))!.Errores.Should().Contain(e =>
            e.Campo == "datosContacto.direccion" &&
            e.Mensaje == "La dirección debe tener al menos 5 caracteres.");
    }

    [Fact]
    public async Task PostUsuarios_Participante_DesdeWeb_Retorna400()
    {
        ConfigurarKeycloakOk();

        var dto = DtoOperador();
        dto.TipoUsuario = RolUsuario.Participante;

        var respuesta = await _cliente.SendAsync(Solicitud(dto, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerErroresAsync(respuesta))!.Errores.Any(e => e.Campo == "tipoUsuario")
            .Should().BeTrue();
    }

    [Fact]
    public async Task PostUsuarios_Operador_TelefonoDuplicado_Retorna400()
    {
        ConfigurarKeycloakOk();
        var telefono = TelefonoUnico();

        var primero = DtoOperador();
        primero.DatosContacto = new DatosContactoDto { Direccion = "Caracas, Venezuela", Telefono = telefono };
        (await _cliente.SendAsync(Solicitud(primero, "Administrador")))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = DtoOperador();
        segundo.DatosContacto = new DatosContactoDto { Direccion = "Maracay, Venezuela", Telefono = telefono };
        var respuesta = await _cliente.SendAsync(Solicitud(segundo, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerErroresAsync(respuesta))!.Errores.Any(e => e.Campo == "datosContacto.telefono")
            .Should().BeTrue();
    }

    [Fact]
    public async Task PostUsuarios_Operador_EnviaNombreYApellidoAKeycloak()
    {
        ConfigurarKeycloakOk($"kc-op-{Guid.NewGuid():N}");
        DatosCreacionUsuarioIdentidad? capturado = null;
        _fabrica.MockProveedor
            .Setup(p => p.CrearUsuarioAsync(
                It.IsAny<DatosCreacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .Callback<DatosCreacionUsuarioIdentidad, CancellationToken>((d, _) => capturado = d)
            .ReturnsAsync("kc-op-firstlast");

        var dto = DtoOperador();
        dto.Nombre = "Angelo";
        dto.Apellido = "Di Martino";

        var respuesta = await _cliente.SendAsync(Solicitud(dto, "Administrador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        capturado.Should().NotBeNull();
        capturado!.Nombre.Should().Be("Angelo");
        capturado.Apellido.Should().Be("Di Martino");
        capturado.NombreUsuario.Should().Be(dto.NombreUsuario);
        capturado.Correo.Should().Be(dto.Correo.ToLowerInvariant());
    }

    private sealed class RespuestaErrorValidacion
    {
        public string? Codigo { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<ErrorPorCampo> Errores { get; set; } = new();
    }

    private sealed class ErrorPorCampo
    {
        public string Campo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
