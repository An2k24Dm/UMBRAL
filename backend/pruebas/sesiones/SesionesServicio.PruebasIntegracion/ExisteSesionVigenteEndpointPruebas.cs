using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU33 — Pruebas de integración del endpoint que sirve a juegos-servicio
// para bloquear la desactivación de contenido con sesiones vigentes.
public sealed class ExisteSesionVigenteEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;

    public ExisteSesionVigenteEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private HttpClient ClienteConRol(string? rol)
    {
        var cliente = _fabrica.CreateClient();
        if (rol is not null)
        {
            cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
            cliente.DefaultRequestHeaders.Add(
                AuthHandlerPruebas.CabeceraIdKeycloak,
                "11111111-1111-1111-1111-111111111111");
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "token-de-prueba");
        }
        return cliente;
    }

    private async Task SembrarSesionAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, EstadoSesion estado)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();

        var sesion = Sesion.Rehidratar(
            id: Guid.NewGuid(),
            nombre: $"Sesión {estado}",
            tipoJuego: tipoJuego,
            contenidoJuegoId: contenidoJuegoId,
            modo: ModoSesion.Individual,
            estado: estado,
            fechaProgramada: new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc),
            creadaPorUsuarioId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            fechaCreacion: new DateTime(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc));

        ctx.Sesiones.Add(SesionesMapeador.HaciaModelo(sesion));
        await ctx.SaveChangesAsync();
    }

    private static string Ruta(TipoJuego tipoJuego, Guid contenidoJuegoId)
        => $"/api/sesiones/contenidos/{tipoJuego}/{contenidoJuegoId}/existe-vigente";

    [Fact]
    public async Task SinToken_Devuelve401()
    {
        var cliente = _fabrica.CreateClient();
        var respuesta = await cliente.GetAsync(Ruta(TipoJuego.Trivia, Guid.NewGuid()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Participante_Devuelve403()
    {
        var cliente = ClienteConRol("Participante");
        var respuesta = await cliente.GetAsync(Ruta(TipoJuego.Trivia, Guid.NewGuid()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task RolPermitido_Devuelve200(string rol)
    {
        var cliente = ClienteConRol(rol);
        var respuesta = await cliente.GetAsync(Ruta(TipoJuego.Trivia, Guid.NewGuid()));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConSesionProgramada_DevuelveExisteTrue()
    {
        var contenidoId = Guid.NewGuid();
        await SembrarSesionAsync(TipoJuego.Trivia, contenidoId, EstadoSesion.Programada);

        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.GetAsync(Ruta(TipoJuego.Trivia, contenidoId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ExisteSesionVigenteRespuestaDto>();
        cuerpo!.Existe.Should().BeTrue();
    }

    [Fact]
    public async Task SoloConSesionesFinalizadasOCanceladas_DevuelveExisteFalse()
    {
        var contenidoId = Guid.NewGuid();
        await SembrarSesionAsync(TipoJuego.Trivia, contenidoId, EstadoSesion.Finalizada);
        await SembrarSesionAsync(TipoJuego.Trivia, contenidoId, EstadoSesion.Cancelada);

        var cliente = ClienteConRol("Operador");
        var respuesta = await cliente.GetAsync(Ruta(TipoJuego.Trivia, contenidoId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ExisteSesionVigenteRespuestaDto>();
        cuerpo!.Existe.Should().BeFalse();
    }

    [Fact]
    public async Task TipoJuegoInvalido_Devuelve400()
    {
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.GetAsync(
            $"/api/sesiones/contenidos/Inventado/{Guid.NewGuid()}/existe-vigente");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ContenidoJuegoIdVacio_Devuelve400()
    {
        var cliente = ClienteConRol("Administrador");
        // Guid.Empty se serializa como "00000000-0000-0000-0000-000000000000"
        // y el endpoint lo rechaza con BadRequest.
        var respuesta = await cliente.GetAsync(
            $"/api/sesiones/contenidos/Trivia/{Guid.Empty}/existe-vigente");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GuidMalFormado_Devuelve404()
    {
        // El constraint :guid del routing rechaza antes de llegar al action,
        // así que no llega como BadRequest sino como 404 de ruteo.
        var cliente = ClienteConRol("Administrador");
        var respuesta = await cliente.GetAsync(
            "/api/sesiones/contenidos/Trivia/no-es-guid/existe-vigente");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
