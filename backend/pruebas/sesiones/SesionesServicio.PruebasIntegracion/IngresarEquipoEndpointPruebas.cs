using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU47 — POST /api/sesiones/{sesionId}/equipos/{equipoId}/ingresar. Un
// Participante ingresa a un equipo disponible de una sesión grupal En
// Preparación; los equipos privados exigen la contraseña del líder.
public class IngresarEquipoEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Únicos por test: evita que la participación única cruce escenarios.
    private readonly Guid IdLider = Guid.NewGuid();
    private readonly Guid IdParticipante = Guid.NewGuid();

    private readonly FabricaApiPruebas _fabrica;

    public IngresarEquipoEndpointPruebas(FabricaApiPruebas fabrica)
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

    private async Task<Guid> CrearSesionGrupalEnPreparacionAsync(
        int maximoPorEquipo = 3)
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", new CrearSesionSolicitudDto
        {
            Nombre = "Sesión grupal HU47",
            Descripcion = "Demo",
            Modo = "Grupal",
            FechaProgramada = DateTime.UtcNow.AddHours(2),
            MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
            MaximoEquipos = 3,
            MaximoParticipantesPorEquipo = maximoPorEquipo
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

    private async Task<Guid> CrearEquipoAsync(
        Guid sesionId, string nombre = "Rojo",
        TipoEquipoDto tipo = TipoEquipoDto.Publico, string? contrasena = null)
    {
        var lider = ClienteConRol("Participante", IdLider);
        var creado = await (await lider.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos",
                new CrearEquipoDto { Nombre = nombre, Tipo = tipo, Contrasena = contrasena }))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);
        return creado!.Id;
    }

    private static StringContent Cuerpo(string? contrasena = null)
        => new(
            JsonSerializer.Serialize(new { contrasena }),
            System.Text.Encoding.UTF8,
            "application/json");

    // ---- Éxitos ----

    [Fact]
    public async Task EquipoPublico_Responde200_YAgregaIntegrante()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(sesionId);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content
            .ReadFromJsonAsync<IngresarEquipoRespuestaDto>(OpcionesJson);
        dto!.EquipoId.Should().Be(equipoId);
        dto.EsMiEquipo.Should().BeTrue();
        dto.CantidadParticipantes.Should().Be(2);

        // El detalle del equipo refleja al nuevo integrante.
        var detalle = await (await participante.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Participantes.Should().HaveCount(2);
        detalle.Participantes.Should().Contain(p => !p.EsLider);
        detalle.EsMiEquipo.Should().BeTrue();
    }

    [Fact]
    public async Task EquipoPrivado_ContrasenaCorrecta_Responde200()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(
            sesionId, "Azul", TipoEquipoDto.Privado, "secreta123");

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar",
            Cuerpo("secreta123"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content
            .ReadFromJsonAsync<IngresarEquipoRespuestaDto>(OpcionesJson);
        dto!.CantidadParticipantes.Should().Be(2);

        // Nunca se expone contraseña ni hash en la respuesta.
        var cuerpo = (await respuesta.Content.ReadAsStringAsync()).ToLowerInvariant();
        cuerpo.Should().NotContain("contrasena");
        cuerpo.Should().NotContain("hash");
    }

    // ---- Contraseña ----

    [Fact]
    public async Task EquipoPrivado_SinContrasena_Responde400()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(
            sesionId, "Azul", TipoEquipoDto.Privado, "secreta123");

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EquipoPrivado_ContrasenaIncorrecta_Responde403()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(
            sesionId, "Azul", TipoEquipoDto.Privado, "secreta123");

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar",
            Cuerpo("incorrecta"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Reglas de negocio ----

    [Fact]
    public async Task EquipoLleno_Responde409()
    {
        // Capacidad 2: líder + un integrante = lleno; el tercero es rechazado.
        var sesionId = await CrearSesionGrupalEnPreparacionAsync(maximoPorEquipo: 2);
        var equipoId = await CrearEquipoAsync(sesionId);

        (await ClienteConRol("Participante", Guid.NewGuid()).PostAsync(
                $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo()))
            .EnsureSuccessStatusCode();

        var tercero = ClienteConRol("Participante", IdParticipante);
        var respuesta = await tercero.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SesionIndividual_Responde409()
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", new CrearSesionSolicitudDto
        {
            Nombre = "Individual HU47",
            Descripcion = "Demo",
            Modo = "Individual",
            FechaProgramada = DateTime.UtcNow.AddHours(2),
            MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
            MaximoParticipantes = 10
        });
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);
        await CambiarEstadoAsync(creado!.Id, EstadoSesion.EnPreparacion);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{creado.Id}/equipos/{Guid.NewGuid()}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task SesionNoEnPreparacion_Responde409(EstadoSesion estado)
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(sesionId);
        await CambiarEstadoAsync(sesionId, estado);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- Autorización ----

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task RolNoParticipante_Responde403(string rol)
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(sesionId);

        var cliente = ClienteConRol(rol);
        var respuesta = await cliente.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SinToken_Responde401()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var equipoId = await CrearEquipoAsync(sesionId);

        var anonimo = _fabrica.CreateClient();
        var respuesta = await anonimo.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- No encontrados ----

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{Guid.NewGuid()}/equipos/{Guid.NewGuid()}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsync(
            $"/api/sesiones/{sesionId}/equipos/{Guid.NewGuid()}/ingresar", Cuerpo());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
