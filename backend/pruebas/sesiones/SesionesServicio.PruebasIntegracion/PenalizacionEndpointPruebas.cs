using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU52 — Aplicar penalización. Acción exclusiva del Operador creador; sesión
// Activa o Pausada; respuesta 202 Accepted. Verifica códigos HTTP, persistencia
// de la penalización y el registro en el Outbox.
public class PenalizacionEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Guid IdParticipante = Guid.NewGuid();
    private readonly FabricaApiPruebas _fabrica;

    public PenalizacionEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private sealed record PenalizacionEncoladaResp(Guid PenalizacionId, Guid EventoId, string Estado);
    private static object Cuerpo(int puntos, string? motivo) => new { puntos, motivo };

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
        Nombre = "Sesión individual HU52",
        Descripcion = "Demo",
        Modo = "Individual",
        FechaProgramada = DateTime.UtcNow.AddHours(2),
        MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
        MaximoParticipantes = 10
    };

    private static CrearSesionSolicitudDto DtoGrupal() => new()
    {
        Nombre = "Sesión grupal HU52",
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

    private async Task<(Guid SesionId, Guid ParticipanteSesionId)> SesionIndividualActivaAsync()
    {
        var sesionId = await CrearSesionAsync(DtoIndividual());
        var pid = await IngresarParticipanteAsync(sesionId, IdParticipante);
        await CambiarEstadoAsync(sesionId, EstadoSesion.Activa);
        return (sesionId, pid);
    }

    private async Task<(Guid SesionId, Guid EquipoId)> SesionGrupalActivaAsync()
    {
        var sesionId = await CrearSesionAsync(DtoGrupal());
        var equipoId = await CrearEquipoAsync(sesionId, IdParticipante);
        await CambiarEstadoAsync(sesionId, EstadoSesion.Activa);
        return (sesionId, equipoId);
    }

    // ---------------- Participante (individual) ----------------

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task PenalizarParticipante_OperadorDueno_Responde202_YPersiste(EstadoSesion estado)
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();
        await CambiarEstadoAsync(sesionId, estado);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(5, "  Incumplió una regla  "));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<PenalizacionEncoladaResp>(OpcionesJson);
        cuerpo!.Estado.Should().Be("Pendiente");
        cuerpo.PenalizacionId.Should().NotBe(Guid.Empty);
        cuerpo.EventoId.Should().NotBe(Guid.Empty);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        var fila = await ctx.Penalizaciones.FirstAsync(p => p.Id == cuerpo.PenalizacionId);
        fila.SesionId.Should().Be(sesionId);
        fila.ParticipanteSesionId.Should().Be(pid);
        fila.EquipoId.Should().BeNull();
        fila.Puntos.Should().Be(5);
        fila.Motivo.Should().Be("Incumplió una regla"); // Trim
        fila.OperadorIdentidadId.Should().Be(FabricaApiPruebas.IdOperadorPrueba);
        fila.TipoObjetivo.Should().Be((int)TipoObjetivoPenalizacion.Participante);
        fila.EstadoProcesamiento.Should().Be((int)EstadoProcesamientoPenalizacion.Pendiente);

        (await ctx.OutboxRanking.AnyAsync(m =>
            m.Id == cuerpo.EventoId
            && m.RoutingKey == "sesion.penalizacion_aplicada"
            && m.Estado == "Pendiente")).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task PenalizarParticipante_PuntosInvalidos_Responde400(int puntos)
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(puntos, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PenalizarParticipante_MotivoInvalido_Responde400(string motivo)
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(5, motivo));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PenalizarParticipante_PuntosDecimales_Responde400()
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            new { puntos = 5.5, motivo = "Motivo" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PenalizarParticipante_SinToken_Responde401()
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var anonimo = _fabrica.CreateClient();
        var respuesta = await anonimo.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PenalizarParticipante_ComoAdministrador_Responde403()
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var admin = ClienteConRol("Administrador");
        var respuesta = await admin.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PenalizarParticipante_OperadorNoDueno_Responde403()
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var otroOperador = ClienteConRol("Operador", Guid.NewGuid());
        var respuesta = await otroOperador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PenalizarParticipante_ObjetivoInexistente_Responde404()
    {
        var (sesionId, _) = await SesionIndividualActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{Guid.NewGuid()}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PenalizarParticipante_SesionInexistente_Responde404()
    {
        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{Guid.NewGuid()}/participantes/{Guid.NewGuid()}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task PenalizarParticipante_EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();
        await CambiarEstadoAsync(sesionId, estado);

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{pid}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PenalizarParticipante_EndpointConSesionGrupal_Responde400()
    {
        var (sesionId, equipoId) = await SesionGrupalActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/participantes/{equipoId}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------- Equipo (grupal) ----------------

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task PenalizarEquipo_OperadorDueno_Responde202_YPersiste(int puntos)
    {
        var (sesionId, equipoId) = await SesionGrupalActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/penalizaciones",
            Cuerpo(puntos, "Penalización al equipo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<PenalizacionEncoladaResp>(OpcionesJson);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        var fila = await ctx.Penalizaciones.FirstAsync(p => p.Id == cuerpo!.PenalizacionId);
        fila.EquipoId.Should().Be(equipoId);
        fila.ParticipanteSesionId.Should().BeNull();
        fila.Puntos.Should().Be(puntos);
        fila.TipoObjetivo.Should().Be((int)TipoObjetivoPenalizacion.Equipo);

        (await ctx.OutboxRanking.AnyAsync(m =>
            m.Id == cuerpo!.EventoId
            && m.RoutingKey == "sesion.penalizacion_aplicada")).Should().BeTrue();
    }

    [Fact]
    public async Task PenalizarEquipo_EndpointConSesionIndividual_Responde400()
    {
        var (sesionId, pid) = await SesionIndividualActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{pid}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PenalizarEquipo_Inexistente_Responde404()
    {
        var (sesionId, _) = await SesionGrupalActivaAsync();

        var operador = ClienteConRol("Operador");
        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{Guid.NewGuid()}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PenalizarEquipo_ComoParticipante_Responde403()
    {
        var (sesionId, equipoId) = await SesionGrupalActivaAsync();

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/penalizaciones",
            Cuerpo(5, "Motivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
