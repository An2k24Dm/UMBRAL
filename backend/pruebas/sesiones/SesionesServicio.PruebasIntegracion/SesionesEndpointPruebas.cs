using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.PruebasIntegracion;

public class SesionesEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FabricaApiPruebas _fabrica;

    public SesionesEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private HttpClient ClienteConRol(string rol, Guid? idKeycloak = null)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
        cliente.DefaultRequestHeaders.Add(
            AuthHandlerPruebas.CabeceraIdKeycloak,
            (idKeycloak ?? FabricaApiPruebas.IdOperadorPrueba).ToString());
        return cliente;
    }

    private static CrearSesionSolicitudDto DtoValido(List<Guid>? misiones = null) => new()
    {
        Nombre = "Sesión de integración",
        Descripcion = "Demo",
        Modo = "Individual",
        FechaProgramada = DateTime.UtcNow.AddHours(2),
        MisionesIds = misiones ?? new List<Guid> { FabricaApiPruebas.IdMisionActiva }
    };

    [Fact]
    public async Task Operador_CreaSesion_Responde201YDatos()
    {
        var cliente = ClienteConRol("Operador");

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);
        cuerpo.Should().NotBeNull();
        cuerpo!.Estado.Should().Be("Programada");
        cuerpo.CodigoAcceso.Should().Be(FabricaApiPruebas.CodigoAccesoPrueba);
        cuerpo.MisionesIds.Should().Equal(new[] { FabricaApiPruebas.IdMisionActiva });
    }

    [Fact]
    public async Task Administrador_RecibeForbidden()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participante_RecibeForbidden()
    {
        var cliente = ClienteConRol("Participante");
        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SinToken_RecibeUnauthorized()
    {
        var cliente = _fabrica.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConMasDeCincoMisiones_Responde400()
    {
        var cliente = ClienteConRol("Operador");
        var seis = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/sesiones", DtoValido(seis));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConMisionInactiva_Responde409()
    {
        var cliente = ClienteConRol("Operador");
        var dto = DtoValido(new List<Guid> { FabricaApiPruebas.IdMisionInactiva });

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ConMisionInexistente_Responde404()
    {
        var cliente = ClienteConRol("Operador");
        var dto = DtoValido(new List<Guid> { FabricaApiPruebas.IdMisionInexistente });

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConFechaPasada_Responde400()
    {
        var cliente = ClienteConRol("Operador");
        var dto = DtoValido();
        dto.FechaProgramada = DateTime.UtcNow.AddHours(-1);

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListarSesiones_OperadorVeSoloLasPropias()
    {
        // Operador A crea una sesión.
        var clienteA = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await clienteA.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();

        // Operador B consulta el listado: no debe ver la sesión de A.
        var clienteB = ClienteConRol("Operador", FabricaApiPruebas.IdOtroOperador);
        var respuesta = await clienteB.GetAsync("/api/sesiones");
        respuesta.EnsureSuccessStatusCode();

        var listado = await respuesta.Content
            .ReadFromJsonAsync<List<SesionListadoDto>>(OpcionesJson);
        listado.Should().NotBeNull();
        listado!.Should().NotContain(s => s.OperadorCreadorId == FabricaApiPruebas.IdOperadorPrueba);
    }

    [Fact]
    public async Task ListarSesiones_AdministradorVeTodo()
    {
        var clienteA = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await clienteA.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();

        var clienteAdmin = ClienteConRol("Administrador");
        var respuesta = await clienteAdmin.GetAsync("/api/sesiones");
        respuesta.EnsureSuccessStatusCode();

        var listado = await respuesta.Content
            .ReadFromJsonAsync<List<SesionListadoDto>>(OpcionesJson);
        listado.Should().NotBeNullOrEmpty();
    }
}
