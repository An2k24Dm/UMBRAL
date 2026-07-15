using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU45 — DELETE /api/sesiones/{sesionId}/equipos/{equipoId}/participantes/
// {participanteSesionId}/expulsar. El líder del equipo o el Operador dueño
// expulsan a un integrante; el expulsado queda fuera de la sesión grupal.
public class ExpulsarParticipanteEquipoEndpointPruebas
    : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Únicos por test: evita que la participación única cruce escenarios.
    private readonly Guid IdLider = Guid.NewGuid();
    private readonly Guid IdMiembro = Guid.NewGuid();

    private readonly FabricaApiPruebas _fabrica;

    public ExpulsarParticipanteEquipoEndpointPruebas(FabricaApiPruebas fabrica)
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

    private async Task<Guid> CrearSesionGrupalEnPreparacionAsync()
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", new CrearSesionSolicitudDto
        {
            Nombre = "Sesión grupal HU45",
            Descripcion = "Demo",
            Modo = "Grupal",
            FechaProgramada = DateTime.UtcNow.AddHours(2),
            MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
            MaximoEquipos = 3,
            MaximoParticipantesPorEquipo = 3
        });
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);
        await CambiarEstadoAsync(creado!.Id, EstadoSesion.EnPreparacion);
        return creado.Id;
    }

    private async Task CambiarEstadoAsync(Guid sesionId, EstadoSesion estado)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        var modelo = await ctx.Sesiones.FirstAsync(s => s.Id == sesionId);
        modelo.Estado = estado;
        await ctx.SaveChangesAsync();
    }

    // Prepara sesión grupal EnPreparacion con equipo (líder + miembro) y
    // devuelve los ids de participación de ambos.
    private async Task<(Guid SesionId, Guid EquipoId, Guid LiderSesionId, Guid MiembroSesionId)>
        PrepararEquipoConMiembroAsync()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();

        var lider = ClienteConRol("Participante", IdLider);
        var creado = await (await lider.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos",
                new CrearEquipoDto { Nombre = "Rojo", Tipo = TipoEquipoDto.Publico }))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);
        var equipoId = creado!.Id;

        var miembro = ClienteConRol("Participante", IdMiembro);
        (await miembro.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar",
                new { contrasena = (string?)null }))
            .EnsureSuccessStatusCode();

        var detalle = await (await lider.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        var liderSesionId = detalle!.Participantes.Single(p => p.EsLider).ParticipanteSesionId;
        var miembroSesionId = detalle.Participantes.Single(p => !p.EsLider).ParticipanteSesionId;

        return (sesionId, equipoId, liderSesionId, miembroSesionId);
    }

    private static string Ruta(Guid sesionId, Guid equipoId, Guid participanteSesionId)
        => $"/api/sesiones/{sesionId}/equipos/{equipoId}" +
           $"/participantes/{participanteSesionId}/expulsar";

    // ---- Éxitos ----

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task OperadorDueno_Expulsa_Responde204(EstadoSesion estado)
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();
        await CambiarEstadoAsync(sesionId, estado);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(Ruta(sesionId, equipoId, miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        (await ctx.Participantes.AnyAsync(p => p.Id == miembroSesionId)).Should().BeFalse();
    }

    [Fact]
    public async Task Lider_ExpulsaIntegrante_Responde204_YDetalleNoLoMuestra()
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();

        var lider = ClienteConRol("Participante", IdLider);
        var respuesta = await lider.DeleteAsync(Ruta(sesionId, equipoId, miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detalle = await (await lider.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Participantes.Should().ContainSingle(p => p.EsLider);
        detalle.Participantes.Should().NotContain(
            p => p.ParticipanteSesionId == miembroSesionId);
    }

    [Fact]
    public async Task OperadorExpulsaLider_ElOtroIntegranteQuedaComoLider()
    {
        var (sesionId, equipoId, liderSesionId, miembroSesionId) =
            await PrepararEquipoConMiembroAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(Ruta(sesionId, equipoId, liderSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detalle = await (await operador.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Participantes.Should().ContainSingle();
        detalle.Participantes.Single().ParticipanteSesionId.Should().Be(miembroSesionId);
        detalle.Participantes.Single().EsLider.Should().BeTrue();
        detalle.LiderParticipanteId.Should().Be(miembroSesionId);
    }

    [Fact]
    public async Task Expulsado_QuedaLibre_PuedeCrearOtroEquipo()
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();

        (await ClienteConRol("Operador").DeleteAsync(
                Ruta(sesionId, equipoId, miembroSesionId)))
            .EnsureSuccessStatusCode();

        // El expulsado quedó fuera de la sesión grupal: puede crear equipo.
        var expulsado = ClienteConRol("Participante", IdMiembro);
        var respuesta = await expulsado.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos",
            new CrearEquipoDto { Nombre = "Azul", Tipo = TipoEquipoDto.Publico });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- Autorización ----

    [Fact]
    public async Task ParticipanteNoLider_Responde403()
    {
        var (sesionId, equipoId, liderSesionId, _) = await PrepararEquipoConMiembroAsync();

        var miembro = ClienteConRol("Participante", IdMiembro);
        var respuesta = await miembro.DeleteAsync(Ruta(sesionId, equipoId, liderSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LiderNoPuedeExpulsarseASiMismo_Responde409()
    {
        var (sesionId, equipoId, liderSesionId, _) = await PrepararEquipoConMiembroAsync();

        var lider = ClienteConRol("Participante", IdLider);
        var respuesta = await lider.DeleteAsync(Ruta(sesionId, equipoId, liderSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Administrador_Responde403()
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();

        var admin = ClienteConRol("Administrador");
        var respuesta = await admin.DeleteAsync(Ruta(sesionId, equipoId, miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OperadorNoDueno_Responde403()
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();

        var otroOperador = ClienteConRol("Operador", Guid.NewGuid());
        var respuesta = await otroOperador.DeleteAsync(
            Ruta(sesionId, equipoId, miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SinToken_Responde401()
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();

        var anonimo = _fabrica.CreateClient();
        var respuesta = await anonimo.DeleteAsync(Ruta(sesionId, equipoId, miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Reglas de estado ----

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var (sesionId, equipoId, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();
        await CambiarEstadoAsync(sesionId, estado);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(Ruta(sesionId, equipoId, miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- No encontrados ----

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var (sesionId, _, _, miembroSesionId) = await PrepararEquipoConMiembroAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            Ruta(sesionId, Guid.NewGuid(), miembroSesionId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ParticipanteInexistente_Responde404()
    {
        var (sesionId, equipoId, _, _) = await PrepararEquipoConMiembroAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            Ruta(sesionId, equipoId, Guid.NewGuid()));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
