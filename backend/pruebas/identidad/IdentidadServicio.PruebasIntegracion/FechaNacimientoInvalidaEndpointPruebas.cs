using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

// Cuando el cliente envía una fecha imposible (p. ej. "2000-12-56") o de
// formato no válido, el backend debe responder 400 con el formato estándar
// { codigo: "VALIDACION", mensaje, errores: [{ campo: "fechaNacimiento", ... }] }
// y nunca 500 ni stacktrace. Esto cubre HU03 (registro), HU09 (Operador) y
// HU10 (Participante).
public class FechaNacimientoInvalidaEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public FechaNacimientoInvalidaEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    // Envía un body donde fechaNacimiento tiene un valor inválido como string.
    private static StringContent ConstruirBodyRegistroConFecha(string fechaCruda)
    {
        // Construido a mano para forzar el valor exacto en JSON (no podemos
        // ponerlo en un DateTime de C# porque es inválido).
        var json = $@"{{
            ""alias"": ""pablito99"",
            ""nombreUsuario"": ""pablo_dt"",
            ""correo"": ""pablo_dt@umbral.com"",
            ""contrasena"": ""Abc1*"",
            ""nombre"": ""Pablo"",
            ""apellido"": ""Participante"",
            ""sexo"": ""Masculino"",
            ""fechaNacimiento"": ""{fechaCruda}"",
            ""datosContacto"": {{
                ""direccion"": ""Av. Caracas, Caracas"",
                ""telefono"": ""04141111111""
            }}
        }}";
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [Theory]
    [InlineData("2000-12-56")] // día imposible
    [InlineData("2000-15-10")] // mes imposible
    [InlineData("no-es-fecha")] // basura
    public async Task RegistrarParticipante_FechaInvalida_Retorna400Controlado(string fechaCruda)
    {
        var respuesta = await _cliente.PostAsync(
            "/api/usuarios/participantes/registro",
            ConstruirBodyRegistroConFecha(fechaCruda));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadAsStringAsync();

        // No es 500 ni mensaje genérico.
        json.Should().NotContain("Ocurrió un error inesperado");
        json.Should().NotContain("ERROR_INTERNO");

        // Formato estándar del proyecto.
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("codigo").GetString().Should().Be("VALIDACION");
        var errores = doc.RootElement.GetProperty("errores");
        errores.GetArrayLength().Should().BeGreaterThan(0);
        var campos = new List<string?>();
        var mensajes = new List<string?>();
        foreach (var elem in errores.EnumerateArray())
        {
            campos.Add(elem.GetProperty("campo").GetString());
            mensajes.Add(elem.GetProperty("mensaje").GetString());
        }
        campos.Should().Contain("fechaNacimiento");
        mensajes.Should().Contain("La fecha de nacimiento no tiene un formato válido.");
    }

    [Fact]
    public async Task RegistrarParticipante_FechaFutura_Retorna400ConErrorDeNegocio()
    {
        // Fecha futura sí parsea correctamente: cae en la regla de negocio
        // ReglasValidacionUsuario.ValidarFechaNacimiento → "no puede ser futura".
        var futuro = DateTime.UtcNow.AddYears(2);
        var body = ConstruirBodyRegistroConFecha(futuro.ToString("yyyy-MM-dd"));

        var respuesta = await _cliente.PostAsync(
            "/api/usuarios/participantes/registro", body);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("\"codigo\":\"VALIDACION\"");
        json.Should().Contain("fechaNacimiento");
        json.Should().Contain("no puede ser futura");
    }

    [Fact]
    public async Task RegistrarParticipante_MenorDe18_Retorna400ConErrorDeNegocio()
    {
        // Menos de 18 años respecto a la fecha sembrada del reloj de pruebas
        // (2026-05-17). 2020-01-01 → 6 años.
        var body = ConstruirBodyRegistroConFecha("2020-01-01");

        var respuesta = await _cliente.PostAsync(
            "/api/usuarios/participantes/registro", body);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("\"codigo\":\"VALIDACION\"");
        json.Should().Contain("fechaNacimiento");
        json.Should().Contain("al menos 18");
    }

    [Fact]
    public async Task ModificarParticipante_FechaInvalida_Retorna400Controlado()
    {
        // Cuerpo PATCH con fechaNacimiento = "2000-12-56". El binding falla
        // antes de llegar al manejador, así que se traduce con el factory.
        var json = @"{ ""fechaNacimiento"": ""2000-12-56"" }";
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Patch, "/api/usuarios/participantes/perfil")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Participante");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak,
            FabricaApiPruebas.IdKeycloakParticipanteSembrado);

        var respuesta = await _cliente.SendAsync(solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().NotContain("ERROR_INTERNO");
        cuerpo.Should().Contain("\"codigo\":\"VALIDACION\"");
        cuerpo.Should().Contain("fechaNacimiento");
        cuerpo.Should().Contain("La fecha de nacimiento no tiene un formato válido.");
    }

    [Fact]
    public async Task ModificarOperador_FechaInvalida_Retorna400Controlado()
    {
        // HU09 — desde el panel del Administrador. Misma traducción, distinto rol.
        var json = @"{ ""fechaNacimiento"": ""2000-12-56"" }";
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Patch, $"/api/usuarios/operadores/{FabricaApiPruebas.IdOperadorSembrado}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");

        var respuesta = await _cliente.SendAsync(solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().Contain("\"codigo\":\"VALIDACION\"");
        cuerpo.Should().Contain("fechaNacimiento");
    }

    [Fact]
    public async Task RegistrarParticipante_FechaValida_FuncionaIgualQueAntes()
    {
        // Garantiza que el cambio no rompe el camino feliz de HU03.
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.CrearUsuarioAsync(
                It.IsAny<DatosCreacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-fecha-ok");
        _fabrica.MockProveedor
            .Setup(p => p.AsignarRolAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new RegistrarParticipanteDto
        {
            Alias = "fechaok99",
            NombreUsuario = "fecha_ok",
            Correo = "fecha_ok@umbral.com",
            Contrasena = "Abc1*",
            Nombre = "Fecha",
            Apellido = "Valida",
            Sexo = "Masculino",
            FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DatosContacto = new DatosContactoDto
            {
                Direccion = "Av. Bolívar, Caracas",
                Telefono = "04141234567"
            }
        };

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/usuarios/participantes/registro", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
