using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Infraestructura.Dependencias;
using IdentidadServicio.Infraestructura.Notificaciones;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IdentidadServicio.PruebasUnitarias.Notificaciones;

// Pruebas de ServicioCorreoGmailApi con un HttpMessageHandler falso.
// NUNCA se llama a Google real: todas las respuestas están simuladas.
public class ServicioCorreoGmailApiPruebas
{
    private const string TokenExitoso =
        "{\"access_token\":\"ya29.token-de-prueba\",\"expires_in\":3599,\"token_type\":\"Bearer\"}";
    private const string EnvioExitoso =
        "{\"id\":\"msg-abc-123\",\"threadId\":\"thr-1\",\"labelIds\":[\"SENT\"]}";

    private static OpcionesGmailApi OpcionesCompletas() => new()
    {
        ClientId = "cid-de-prueba",
        ClientSecret = "csecret-de-prueba",
        RefreshToken = "rtoken-de-prueba",
        RemitenteCorreo = "remitente@umbral.local",
        RemitenteNombre = "UMBRAL"
    };

    private static ServicioCorreoGmailApi CrearServicio(
        ManejadorHttpFalso manejador, OpcionesGmailApi opciones) =>
        new(new HttpClient(manejador),
            Options.Create(opciones),
            NullLogger<ServicioCorreoGmailApi>.Instance);

    // ---------------------------------------------------------------------
    // 1. Configuración incompleta
    // ---------------------------------------------------------------------
    public static IEnumerable<object[]> ConfiguracionesIncompletas()
    {
        var basePruebas = OpcionesCompletas();
        yield return new object[] { new OpcionesGmailApi
        {
            ClientId = "", ClientSecret = basePruebas.ClientSecret,
            RefreshToken = basePruebas.RefreshToken, RemitenteCorreo = basePruebas.RemitenteCorreo
        }};
        yield return new object[] { new OpcionesGmailApi
        {
            ClientId = basePruebas.ClientId, ClientSecret = "",
            RefreshToken = basePruebas.RefreshToken, RemitenteCorreo = basePruebas.RemitenteCorreo
        }};
        yield return new object[] { new OpcionesGmailApi
        {
            ClientId = basePruebas.ClientId, ClientSecret = basePruebas.ClientSecret,
            RefreshToken = "", RemitenteCorreo = basePruebas.RemitenteCorreo
        }};
        yield return new object[] { new OpcionesGmailApi
        {
            ClientId = basePruebas.ClientId, ClientSecret = basePruebas.ClientSecret,
            RefreshToken = basePruebas.RefreshToken, RemitenteCorreo = ""
        }};
    }

    [Theory]
    [MemberData(nameof(ConfiguracionesIncompletas))]
    public async Task Configuracion_incompleta_lanza_excepcion_controlada_y_no_llama_a_google(
        OpcionesGmailApi opciones)
    {
        var manejador = new ManejadorHttpFalso(RespondedorPorDefecto());
        var servicio = CrearServicio(manejador, opciones);

        var acto = () => servicio.EnviarAsync(
            "destino@umbral.local", "Asunto", "Cuerpo", CancellationToken.None);

        await acto.Should().ThrowAsync<ExcepcionEnvioCorreoGmail>();
        manejador.Solicitudes.Should().BeEmpty(); // no se contactó a Google
    }

    // ---------------------------------------------------------------------
    // 2 + 4. Token exitoso y envío exitoso (POST correctos, Bearer, raw base64url)
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Envio_exitoso_hace_los_dos_POST_correctos_con_Bearer_y_raw_base64url()
    {
        var manejador = new ManejadorHttpFalso(RespondedorPorDefecto());
        var servicio = CrearServicio(manejador, OpcionesCompletas());

        await servicio.EnviarAsync(
            "destino@umbral.local", "Asunto de prueba", "Cuerpo de prueba", CancellationToken.None);

        manejador.Solicitudes.Should().HaveCount(2);

        // --- Solicitud de token ---
        var token = manejador.Solicitudes[0];
        token.Solicitud.Method.Should().Be(HttpMethod.Post);
        token.Solicitud.RequestUri!.AbsoluteUri.Should().Be("https://oauth2.googleapis.com/token");
        token.Cuerpo.Should().Contain("grant_type=refresh_token");
        token.Cuerpo.Should().Contain("client_id=cid-de-prueba");
        token.Cuerpo.Should().Contain("refresh_token=rtoken-de-prueba");

        // --- Solicitud de envío ---
        var envio = manejador.Solicitudes[1];
        envio.Solicitud.Method.Should().Be(HttpMethod.Post);
        envio.Solicitud.RequestUri!.AbsoluteUri.Should()
            .Be("https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
        envio.Solicitud.Headers.Authorization!.Scheme.Should().Be("Bearer");
        envio.Solicitud.Headers.Authorization!.Parameter.Should().Be("ya29.token-de-prueba");

        var raw = ExtraerRaw(envio.Cuerpo!);
        raw.Should().MatchRegex("^[A-Za-z0-9_-]+$"); // base64 URL-safe: sin +, /, =
        var mime = DecodificarBase64Url(raw);
        mime.Should().Contain("To: destino@umbral.local");
        mime.Should().Contain("From: UMBRAL <remitente@umbral.local>");
        mime.Should().Contain("Content-Type: text/plain; charset=UTF-8");
        mime.Should().Contain("Cuerpo de prueba");
    }

    // ---------------------------------------------------------------------
    // 3. Token rechazado (400/401): excepción controlada y NO se envía
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Token_rechazado_lanza_excepcion_y_no_ejecuta_el_envio(HttpStatusCode estado)
    {
        var manejador = new ManejadorHttpFalso((solicitud, _) =>
        {
            if (EsToken(solicitud))
                return Json(estado, "{\"error\":\"invalid_grant\"}");
            return Json(HttpStatusCode.OK, EnvioExitoso);
        });
        var servicio = CrearServicio(manejador, OpcionesCompletas());

        var acto = () => servicio.EnviarAsync(
            "destino@umbral.local", "Asunto", "Cuerpo", CancellationToken.None);

        await acto.Should().ThrowAsync<ExcepcionEnvioCorreoGmail>();
        manejador.Solicitudes.Should().HaveCount(1); // solo el token; nunca el envío
        manejador.Solicitudes[0].Solicitud.RequestUri!.AbsoluteUri
            .Should().Contain("oauth2.googleapis.com");
    }

    // ---------------------------------------------------------------------
    // 5. Envío rechazado (400/401/403/500): excepción controlada
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Envio_rechazado_lanza_excepcion_controlada(HttpStatusCode estado)
    {
        var manejador = new ManejadorHttpFalso((solicitud, _) =>
        {
            if (EsToken(solicitud))
                return Json(HttpStatusCode.OK, TokenExitoso);
            return Json(estado, "{\"error\":{\"code\":" + (int)estado + "}}");
        });
        var servicio = CrearServicio(manejador, OpcionesCompletas());

        var acto = () => servicio.EnviarAsync(
            "destino@umbral.local", "Asunto", "Cuerpo", CancellationToken.None);

        await acto.Should().ThrowAsync<ExcepcionEnvioCorreoGmail>();
        manejador.Solicitudes.Should().HaveCount(2); // token OK + intento de envío
    }

    // ---------------------------------------------------------------------
    // 6. UTF-8: acentos y ñ recuperables al decodificar Base64 URL-safe
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Mensaje_soporta_UTF8_y_es_recuperable_desde_base64url()
    {
        const string asunto = "Contraseña temporal · acción requerida ñ";
        const string cuerpo = "Hola Ángel,\nTu contraseña es: Ñoño-123\nGracias.";

        var manejador = new ManejadorHttpFalso(RespondedorPorDefecto());
        var servicio = CrearServicio(manejador, OpcionesCompletas());

        await servicio.EnviarAsync("destiño@umbral.local", asunto, cuerpo, CancellationToken.None);

        var raw = ExtraerRaw(manejador.Solicitudes[1].Cuerpo!);
        var mime = DecodificarBase64Url(raw);

        // El cuerpo va como UTF-8 literal: recuperable directamente, con saltos de línea.
        mime.Should().Contain("Tu contraseña es: Ñoño-123");
        mime.Should().Contain("\r\n"); // saltos de línea normalizados a CRLF

        // El asunto va como encoded-word RFC 2047; se recupera decodificándolo.
        var esperado = "=?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(asunto)) + "?=";
        mime.Should().Contain("Subject: " + esperado);
        DecodificarEncodedWord(esperado).Should().Be(asunto);
    }

    // ---------------------------------------------------------------------
    // 7. Cancelación: no se convierte en error de credenciales
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Cancelacion_se_propaga_como_OperationCanceled_no_como_error_de_credenciales()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // token ya cancelado antes de empezar

        var manejador = new ManejadorHttpFalso(RespondedorPorDefecto());
        var servicio = CrearServicio(manejador, OpcionesCompletas());

        var acto = () => servicio.EnviarAsync(
            "destino@umbral.local", "Asunto", "Cuerpo", cts.Token);

        // Debe ser una cancelación, NO una ExcepcionEnvioCorreoGmail.
        await acto.Should().ThrowAsync<OperationCanceledException>();
    }

    // ---------------------------------------------------------------------
    // 8. Selección del proveedor
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Smtp")]
    [InlineData("smtp")]
    [InlineData("SMTP")]
    public void Selector_sin_config_o_Smtp_resuelve_SMTP(string? proveedor)
    {
        var smtp = Mock.Of<IServicioCorreo>();
        var gmail = Mock.Of<IServicioCorreo>();

        var resuelto = SelectorProveedorCorreo.Resolver(proveedor, () => smtp, () => gmail);

        resuelto.Should().BeSameAs(smtp);
    }

    [Theory]
    [InlineData("GmailApi")]
    [InlineData("gmailapi")]
    public void Selector_GmailApi_resuelve_GmailApi(string proveedor)
    {
        var smtp = Mock.Of<IServicioCorreo>();
        var gmail = Mock.Of<IServicioCorreo>();

        var resuelto = SelectorProveedorCorreo.Resolver(proveedor, () => smtp, () => gmail);

        resuelto.Should().BeSameAs(gmail);
    }

    [Fact]
    public void Selector_proveedor_desconocido_lanza_error_claro()
    {
        var acto = () => SelectorProveedorCorreo.Resolver(
            "Desconocido", () => Mock.Of<IServicioCorreo>(), () => Mock.Of<IServicioCorreo>());

        acto.Should().Throw<InvalidOperationException>()
            .WithMessage("*no soportado*");
    }

    // --- Wiring real de DI (composición en RegistroInfraestructura) ---
    [Theory]
    [InlineData(null, typeof(ServicioCorreoSmtp))]
    [InlineData("Smtp", typeof(ServicioCorreoSmtp))]
    [InlineData("GmailApi", typeof(ServicioCorreoGmailApi))]
    public void DI_resuelve_la_implementacion_segun_EnvioCorreo_Proveedor(
        string? proveedor, Type tipoEsperado)
    {
        using var scope = ConstruirContenedor(proveedor).CreateScope();

        var correo = scope.ServiceProvider.GetRequiredService<IServicioCorreo>();

        correo.Should().BeOfType(tipoEsperado);
    }

    [Fact]
    public void DI_proveedor_desconocido_lanza_error_claro_al_resolver()
    {
        using var scope = ConstruirContenedor("Desconocido").CreateScope();

        var acto = () => scope.ServiceProvider.GetRequiredService<IServicioCorreo>();

        acto.Should().Throw<InvalidOperationException>().WithMessage("*no soportado*");
    }

    private static ServiceProvider ConstruirContenedor(string? proveedor)
    {
        var ajustes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:BaseDatos"] =
                "Host=localhost;Port=5432;Database=umbral_identidad;Username=umbral;Password=umbral123"
        };
        if (proveedor is not null)
            ajustes["EnvioCorreo:Proveedor"] = proveedor;

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(ajustes)
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        return servicios.BuildServiceProvider();
    }

    // ---------------------------------------------------------------------
    // Utilidades de prueba
    // ---------------------------------------------------------------------
    private static Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>
        RespondedorPorDefecto() => (solicitud, _) =>
            EsToken(solicitud)
                ? Json(HttpStatusCode.OK, TokenExitoso)
                : Json(HttpStatusCode.OK, EnvioExitoso);

    private static bool EsToken(HttpRequestMessage solicitud) =>
        solicitud.RequestUri!.AbsoluteUri.Contains("oauth2.googleapis.com");

    private static HttpResponseMessage Json(HttpStatusCode estado, string json) =>
        new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string ExtraerRaw(string jsonEnvio)
    {
        var m = Regex.Match(jsonEnvio, "\"raw\"\\s*:\\s*\"(?<raw>[^\"]*)\"");
        m.Success.Should().BeTrue("el cuerpo del envío debe incluir la propiedad raw");
        return m.Groups["raw"].Value;
    }

    private static string DecodificarBase64Url(string raw)
    {
        var s = raw.Replace('-', '+').Replace('_', '/');
        s = (s.Length % 4) switch { 2 => s + "==", 3 => s + "=", _ => s };
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }

    private static string DecodificarEncodedWord(string encodedWord)
    {
        // Formato: =?UTF-8?B?<base64>?=
        var base64 = encodedWord["=?UTF-8?B?".Length..^"?=".Length];
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

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
            cancellationToken.ThrowIfCancellationRequested();
            string? cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Solicitudes.Add((request, cuerpo));
            return _responder(request, cancellationToken);
        }
    }
}
