using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU44 — Expulsar participante (sesión individual) o equipo (sesión grupal).
// Acción exclusiva del Operador creador. La expulsión es por HTTP; SignalR
// solo notifica el cambio.
public class ExpulsionEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Guid IdParticipante = Guid.NewGuid();
    private readonly FabricaApiPruebas _fabrica;

    public ExpulsionEndpointPruebas(FabricaApiPruebas fabrica)
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

    private static CrearSesionSolicitudDto DtoIndividual() => new()
    {
        Nombre = "Sesión individual HU44",
        Descripcion = "Demo",
        Modo = "Individual",
        FechaProgramada = DateTime.UtcNow.AddHours(2),
        MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
        MaximoParticipantes = 10
    };

    private static CrearSesionSolicitudDto DtoGrupal() => new()
    {
        Nombre = "Sesión grupal HU44",
        Descripcion = "Demo",
        Modo = "Grupal",
        FechaProgramada = DateTime.UtcNow.AddHours(2),
        MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
        MaximoEquipos = 3,
        MaximoParticipantesPorEquipo = 3
    };

    private async Task<Guid> CrearSesionAsync(CrearSesionSolicitudDto dto)
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", dto);
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

    // Ingresa un participante a la sesión individual y devuelve su
    // ParticipanteSesionId (id de la participación local).
    private async Task<Guid> IngresarParticipanteAsync(Guid sesionId, Guid participanteId)
    {
        var participante = ClienteConRol("Participante", participanteId);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/participante/ingresar-individual", null);
        respuesta.EnsureSuccessStatusCode();
        var dto = await respuesta.Content
            .ReadFromJsonAsync<IngresarSesionRespuestaDto>(OpcionesJson);
        return dto!.ParticipacionActual!.ParticipanteSesionId!.Value;
    }

    private async Task<Guid> CrearEquipoAsync(Guid sesionId, Guid liderId, string nombre = "Rojo")
    {
        var lider = ClienteConRol("Participante", liderId);
        var creado = await (await lider.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos",
                new CrearEquipoDto { Nombre = nombre, Tipo = TipoEquipoDto.Publico }))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);
        return creado!.Id;
    }

    // ---- Expulsar participante individual ----

    [Fact]
    public async Task ExpulsarParticipante_OperadorDueno_Responde204_YElimina()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detalleRespuesta = await operador.GetAsync($"/api/sesiones/{sesionId}");
        detalleRespuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await detalleRespuesta.Content
            .ReadFromJsonAsync<SesionDetalleDto>(OpcionesJson);
        detalle!.ParticipantesIndividuales.Should()
            .NotContain(p => p.ParticipanteSesionId == participanteSesionId);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        (await ctx.Participantes.AnyAsync(p => p.Id == participanteSesionId)).Should().BeFalse();
        (await ctx.Sesiones.AnyAsync(s => s.Id == sesionId)).Should().BeTrue();
    }

    [Fact]
    public async Task ExpulsarParticipante_SesionPausada_Responde204()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);
        await CambiarEstadoAsync(sesionId, EstadoSesion.Pausada);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ExpulsarParticipante_ComoParticipante_Responde403()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpulsarParticipante_OperadorNoDueno_Responde403()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);

        var otroOperador = ClienteConRol("Operador", Guid.NewGuid());
        var respuesta = await otroOperador.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpulsarParticipante_Inexistente_Responde404()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{Guid.NewGuid()}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExpulsarParticipante_SesionInexistente_Responde404()
    {
        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{Guid.NewGuid()}/participantes/{Guid.NewGuid()}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task ExpulsarParticipante_EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);
        await CambiarEstadoAsync(sesionId, estado);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExpulsarParticipante_LiberaParticipacion_PermiteIngresarDeNuevo()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);

        (await ClienteConRol("Operador").DeleteAsync(
                $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar"))
            .EnsureSuccessStatusCode();

        // Ya no queda participación activa: puede volver a ingresar.
        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/participante/ingresar-individual", null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- Expulsar equipo grupal ----

    [Fact]
    public async Task ExpulsarEquipo_OperadorDueno_Responde204_YEliminaEquipoYParticipantes()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listadoRespuesta = await operador.GetAsync(
            $"/api/sesiones/{sesionId}/equipos");
        listadoRespuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var equipos = await listadoRespuesta.Content
            .ReadFromJsonAsync<List<EquipoSesionListadoDto>>(OpcionesJson);
        equipos.Should().NotContain(e => e.Id == equipoId);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        (await ctx.Equipos.AnyAsync(e => e.Id == equipoId)).Should().BeFalse();
        (await ctx.Participantes.AnyAsync(p => p.EquipoId == equipoId)).Should().BeFalse();
        (await ctx.Participantes.AnyAsync(p => p.SesionId == sesionId)).Should().BeFalse();
        (await ctx.Sesiones.AnyAsync(s => s.Id == sesionId)).Should().BeTrue();
    }

    [Fact]
    public async Task ExpulsarEquipo_SesionPausada_Responde204()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);
        await CambiarEstadoAsync(sesionId, EstadoSesion.Pausada);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ExpulsarEquipo_ComoParticipante_Responde403()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpulsarEquipo_OperadorNoDueno_Responde403()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);

        var otroOperador = ClienteConRol("Operador", Guid.NewGuid());
        var respuesta = await otroOperador.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpulsarEquipo_Inexistente_Responde404()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{Guid.NewGuid()}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task ExpulsarEquipo_EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);
        await CambiarEstadoAsync(sesionId, estado);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExpulsarEquipo_SesionInexistente_Responde404()
    {
        var operador = ClienteConRol("Operador");
        var respuesta = await operador.DeleteAsync(
            $"/api/sesiones/{Guid.NewGuid()}/equipos/{Guid.NewGuid()}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- El Administrador no puede expulsar (solo lectura) ----

    [Fact]
    public async Task ExpulsarParticipante_ComoAdministrador_Responde403()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var participanteSesionId = await IngresarParticipanteAsync(sesionId, IdParticipante);

        var admin = ClienteConRol("Administrador");
        var respuesta = await admin.DeleteAsync(
            $"/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpulsarEquipo_ComoAdministrador_Responde403()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);

        var admin = ClienteConRol("Administrador");
        var respuesta = await admin.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
