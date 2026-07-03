using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU48 — DELETE /api/sesiones/{sesionId}/abandonar. El propio Participante
// abandona la sesión individual o su equipo en la sesión grupal (solo En
// Preparación). El registro de participación se elimina de la base.
public class AbandonarSesionEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Únicos por test: evita que la participación única cruce escenarios.
    private readonly Guid IdLider = Guid.NewGuid();
    private readonly Guid IdMiembro = Guid.NewGuid();

    private readonly FabricaApiPruebas _fabrica;

    public AbandonarSesionEndpointPruebas(FabricaApiPruebas fabrica)
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

    private async Task<Guid> CrearSesionAsync(bool grupal)
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", new CrearSesionSolicitudDto
        {
            Nombre = grupal ? "Grupal HU48" : "Individual HU48",
            Descripcion = "Demo",
            Modo = grupal ? "Grupal" : "Individual",
            FechaProgramada = DateTime.UtcNow.AddHours(2),
            MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
            MaximoParticipantes = grupal ? null : 5,
            MaximoEquipos = grupal ? 3 : null,
            MaximoParticipantesPorEquipo = grupal ? 3 : null
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

    // Sesión individual EnPreparacion con el miembro ya ingresado.
    private async Task<Guid> PrepararIndividualConMiembroAsync()
    {
        var sesionId = await CrearSesionAsync(grupal: false);
        var miembro = ClienteConRol("Participante", IdMiembro);
        (await miembro.PostAsync(
                $"/api/sesiones/{sesionId}/participante/ingresar-individual", null))
            .EnsureSuccessStatusCode();
        return sesionId;
    }

    // Sesión grupal EnPreparacion con equipo (líder + opcionalmente miembro).
    private async Task<(Guid SesionId, Guid EquipoId)> PrepararGrupalConEquipoAsync(
        bool conMiembro = true)
    {
        var sesionId = await CrearSesionAsync(grupal: true);
        var lider = ClienteConRol("Participante", IdLider);
        var creado = await (await lider.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos",
                new CrearEquipoDto { Nombre = "Rojo", Tipo = TipoEquipoDto.Publico }))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);
        if (conMiembro)
        {
            var miembro = ClienteConRol("Participante", IdMiembro);
            (await miembro.PostAsJsonAsync(
                    $"/api/sesiones/{sesionId}/equipos/{creado!.Id}/ingresar",
                    new { contrasena = (string?)null }))
                .EnsureSuccessStatusCode();
        }
        return (sesionId, creado!.Id);
    }

    // ---- Sesión individual ----

    [Fact]
    public async Task Individual_Abandona_Responde204_YEliminaRegistro()
    {
        var sesionId = await PrepararIndividualConMiembroAsync();

        var miembro = ClienteConRol("Participante", IdMiembro);
        var respuesta = await miembro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // El registro de participación se eliminó de la base.
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        (await ctx.Participantes.AnyAsync(
            p => p.SesionId == sesionId && p.ParticipanteIdentidadId == IdMiembro))
            .Should().BeFalse();

        // Y el detalle ya no lo muestra como inscrito.
        var detalle = await (await miembro.GetAsync(
                $"/api/sesiones/participante/disponibles/{sesionId}"))
            .Content.ReadFromJsonAsync<SesionDetalleMovilDto>(OpcionesJson);
        detalle!.ParticipacionActual.EstaInscrito.Should().BeFalse();
    }

    [Fact]
    public async Task Individual_TrasAbandonar_PuedeVolverAIngresar()
    {
        var sesionId = await PrepararIndividualConMiembroAsync();
        var miembro = ClienteConRol("Participante", IdMiembro);

        (await miembro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar"))
            .EnsureSuccessStatusCode();

        // La participación única ya no lo bloquea: puede reingresar.
        var reingreso = await miembro.PostAsync(
            $"/api/sesiones/{sesionId}/participante/ingresar-individual", null);
        reingreso.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- Sesión grupal ----

    [Fact]
    public async Task Grupal_IntegranteAbandona_Responde204_YSaleDelEquipo()
    {
        var (sesionId, equipoId) = await PrepararGrupalConEquipoAsync();

        var miembro = ClienteConRol("Participante", IdMiembro);
        var respuesta = await miembro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lider = ClienteConRol("Participante", IdLider);
        var detalle = await (await lider.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Participantes.Should().ContainSingle(p => p.EsLider);
        detalle.Participantes.Should().NotContain(
            p => p.ParticipanteIdentidadId == IdMiembro);
    }

    [Fact]
    public async Task Grupal_LiderAbandonaConOtroIntegrante_ElOtroQuedaComoLider()
    {
        var (sesionId, equipoId) = await PrepararGrupalConEquipoAsync();

        var lider = ClienteConRol("Participante", IdLider);
        (await lider.DeleteAsync($"/api/sesiones/{sesionId}/abandonar"))
            .EnsureSuccessStatusCode();

        var miembro = ClienteConRol("Participante", IdMiembro);
        var detalle = await (await miembro.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Participantes.Should().ContainSingle();
        detalle.Participantes.Single().ParticipanteIdentidadId.Should().Be(IdMiembro);
        detalle.Participantes.Single().EsLider.Should().BeTrue();
    }

    [Fact]
    public async Task Grupal_LiderUnicoAbandona_ElEquipoDesaparece()
    {
        var (sesionId, equipoId) = await PrepararGrupalConEquipoAsync(conMiembro: false);

        var lider = ClienteConRol("Participante", IdLider);
        (await lider.DeleteAsync($"/api/sesiones/{sesionId}/abandonar"))
            .EnsureSuccessStatusCode();

        var otro = ClienteConRol("Participante", Guid.NewGuid());
        var equipos = await (await otro.GetAsync($"/api/sesiones/{sesionId}/equipos"))
            .Content.ReadFromJsonAsync<List<EquipoSesionListadoDto>>(OpcionesJson);
        equipos.Should().NotContain(e => e.Id == equipoId);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        (await ctx.Equipos.AnyAsync(e => e.Id == equipoId)).Should().BeFalse();
        (await ctx.Participantes.AnyAsync(p => p.EquipoId == equipoId)).Should().BeFalse();
    }

    [Fact]
    public async Task Grupal_TrasAbandonar_PuedeCrearOtroEquipo()
    {
        var (sesionId, _) = await PrepararGrupalConEquipoAsync();

        var miembro = ClienteConRol("Participante", IdMiembro);
        (await miembro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar"))
            .EnsureSuccessStatusCode();

        var respuesta = await miembro.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos",
            new CrearEquipoDto { Nombre = "Azul", Tipo = TipoEquipoDto.Publico });
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- Estados no permitidos (HU48 solo EnPreparacion) ----

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task Individual_EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var sesionId = await PrepararIndividualConMiembroAsync();
        await CambiarEstadoAsync(sesionId, estado);

        var miembro = ClienteConRol("Participante", IdMiembro);
        var respuesta = await miembro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Activa)]
    public async Task Grupal_EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var (sesionId, _) = await PrepararGrupalConEquipoAsync();
        await CambiarEstadoAsync(sesionId, estado);

        var miembro = ClienteConRol("Participante", IdMiembro);
        var respuesta = await miembro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- Autorización ----

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task RolNoParticipante_Responde403(string rol)
    {
        var sesionId = await PrepararIndividualConMiembroAsync();

        var cliente = ClienteConRol(rol);
        var respuesta = await cliente.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SinToken_Responde401()
    {
        var sesionId = await PrepararIndividualConMiembroAsync();

        var anonimo = _fabrica.CreateClient();
        var respuesta = await anonimo.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- No encontrados ----

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var miembro = ClienteConRol("Participante", IdMiembro);
        var respuesta = await miembro.DeleteAsync(
            $"/api/sesiones/{Guid.NewGuid()}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NoInscrito_Responde404()
    {
        var sesionId = await PrepararIndividualConMiembroAsync();

        var otro = ClienteConRol("Participante", Guid.NewGuid());
        var respuesta = await otro.DeleteAsync($"/api/sesiones/{sesionId}/abandonar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
