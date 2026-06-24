using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU40 — POST /api/sesiones/{sesionId}/equipos. Crear equipo desde el flujo
// del participante sobre una sesión grupal En Preparación.
public class EquiposEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Único por test (xUnit instancia la clase por método). Evita que, con la
    // regla de participación única y la BD compartida del fixture, un test deje
    // al participante "ocupado" para los siguientes.
    private readonly Guid IdParticipante = Guid.NewGuid();

    private readonly FabricaApiPruebas _fabrica;

    public EquiposEndpointPruebas(FabricaApiPruebas fabrica)
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

    private static CrearSesionSolicitudDto DtoGrupal() => new()
    {
        Nombre = "Sesión grupal HU40",
        Descripcion = "Demo",
        Modo = "Grupal",
        FechaProgramada = DateTime.UtcNow.AddHours(2),
        MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
        MaximoEquipos = 2,
        MaximoParticipantesPorEquipo = 3
    };

    // Crea una sesión grupal vía API (Operador) y la deja En Preparación
    // manipulando el estado directamente en la base InMemory, ya que la
    // transición la realiza normalmente el servicio en segundo plano.
    private async Task<Guid> CrearSesionGrupalEnPreparacionAsync(
        CrearSesionSolicitudDto? dto = null)
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", dto ?? DtoGrupal());
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

    private static CrearEquipoDto EquipoPublico(string nombre = "Rojo") => new()
    {
        Nombre = nombre,
        Tipo = TipoEquipoDto.Publico
    };

    private static CrearEquipoDto EquipoPrivado(string nombre = "Azul", string pass = "secreta") => new()
    {
        Nombre = nombre,
        Tipo = TipoEquipoDto.Privado,
        Contrasena = pass
    };

    [Fact]
    public async Task DetalleParticipante_TrasCrearEquipo_ReflejaParticipacion()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        // Antes de crear: no inscrito.
        var antes = await (await participante.GetAsync(
                $"/api/sesiones/participante/disponibles/{sesionId}"))
            .Content.ReadFromJsonAsync<SesionDetalleMovilDto>(OpcionesJson);
        antes!.ParticipacionActual.EstaInscrito.Should().BeFalse();

        (await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .EnsureSuccessStatusCode();

        // Después de crear: inscrito como líder de equipo.
        var despues = await (await participante.GetAsync(
                $"/api/sesiones/participante/disponibles/{sesionId}"))
            .Content.ReadFromJsonAsync<SesionDetalleMovilDto>(OpcionesJson);
        despues!.ParticipacionActual.EstaInscrito.Should().BeTrue();
        despues.ParticipacionActual.Tipo.Should().Be("Equipo");
        despues.ParticipacionActual.EsLider.Should().BeTrue();
        despues.ParticipacionActual.EquipoNombre.Should().Be("Rojo");

        // Otro participante de la misma sesión sigue sin inscribir.
        var otro = ClienteConRol("Participante", Guid.NewGuid());
        var paraOtro = await (await otro.GetAsync(
                $"/api/sesiones/participante/disponibles/{sesionId}"))
            .Content.ReadFromJsonAsync<SesionDetalleMovilDto>(OpcionesJson);
        paraOtro!.ParticipacionActual.EstaInscrito.Should().BeFalse();
    }

    [Fact]
    public async Task CrearEquipoPublico_Responde201()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await respuesta.Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);
        dto!.Nombre.Should().Be("Rojo");
        dto.Tipo.Should().Be("Publico");
        dto.CapacidadMaxima.Should().Be(3);
        dto.CantidadParticipantes.Should().Be(1);
        dto.LiderParticipanteId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CrearEquipoPrivado_Responde201_YPersisteEquipoYLider()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPrivado());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await respuesta.Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();

        var equipo = await ctx.Equipos.FirstAsync(e => e.Id == dto!.Id);
        equipo.Tipo.Should().Be(TipoEquipo.Privado);
        // No se guarda en texto plano y nunca coincide con la contraseña.
        equipo.ContrasenaHash.Should().NotBeNullOrWhiteSpace();
        equipo.ContrasenaHash.Should().NotContain("secreta");

        var lider = await ctx.Participantes.FirstAsync(p => p.EquipoId == dto!.Id);
        lider.ParticipanteIdentidadId.Should().Be(IdParticipante);
    }

    [Fact]
    public async Task Respuesta_NoIncluyeContrasenaNiHash()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPrivado(pass: "topsecret"));
        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.ToLowerInvariant().Should().NotContain("contrasena");
        cuerpo.ToLowerInvariant().Should().NotContain("hash");
        cuerpo.Should().NotContain("topsecret");
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{Guid.NewGuid()}/equipos", EquipoPublico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ComoOperador_Responde403()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var operador = ClienteConRol("Operador");

        var respuesta = await operador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SesionProgramada_NoEnPreparacion_Responde409()
    {
        // Sin transicionar: la sesión queda Programada.
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", DtoGrupal());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{creado!.Id}/equipos", EquipoPublico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SesionIndividual_Responde409()
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", new CrearSesionSolicitudDto
        {
            Nombre = "Individual HU40",
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
        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{creado.Id}/equipos", EquipoPublico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SuperaMaximoEquipos_Responde409()
    {
        // MaximoEquipos = 2: el tercer equipo debe fallar.
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();

        var p1 = ClienteConRol("Participante", Guid.NewGuid());
        var p2 = ClienteConRol("Participante", Guid.NewGuid());
        var p3 = ClienteConRol("Participante", Guid.NewGuid());

        (await p1.PostAsJsonAsync($"/api/sesiones/{sesionId}/equipos", EquipoPublico("Uno")))
            .EnsureSuccessStatusCode();
        (await p2.PostAsJsonAsync($"/api/sesiones/{sesionId}/equipos", EquipoPublico("Dos")))
            .EnsureSuccessStatusCode();

        var respuesta = await p3.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Tres"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ParticipanteYaEnUnEquipo_Responde409()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        (await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Primero")))
            .EnsureSuccessStatusCode();

        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Segundo"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- HU43: consultar equipos ----

    [Fact]
    public async Task ListarEquipos_Participante_Responde200()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var creador = ClienteConRol("Participante", IdParticipante);
        (await creador.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .EnsureSuccessStatusCode();

        var otro = ClienteConRol("Participante", Guid.NewGuid());
        var respuesta = await otro.GetAsync($"/api/sesiones/{sesionId}/equipos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var equipos = await respuesta.Content
            .ReadFromJsonAsync<List<EquipoSesionListadoDto>>(OpcionesJson);
        equipos.Should().ContainSingle(e => e.Nombre == "Rojo");
        equipos!.Single().CapacidadMaxima.Should().Be(3);
    }

    [Fact]
    public async Task DetalleEquipo_Participante_Responde200_SinSecretos()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPrivado("Azul")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var respuesta = await participante.GetAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content
            .ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Nombre.Should().Be("Azul");
        detalle.Tipo.Should().Be("Privado");
        detalle.Participantes.Should().ContainSingle(p => p.EsLider);
        detalle.Participantes.Single().FechaUnion.Should().NotBe(default);
        detalle.Participantes.Single().Alias.Should().NotBeNullOrWhiteSpace();
        detalle.EsMiEquipo.Should().BeTrue();

        var cuerpo = (await respuesta.Content.ReadAsStringAsync()).ToLowerInvariant();
        cuerpo.Should().NotContain("contrasena");
        cuerpo.Should().NotContain("hash");
        cuerpo.Should().NotContain("secreta");
        cuerpo.Should().NotContain("correo");
        cuerpo.Should().NotContain("telefono");
        cuerpo.Should().NotContain("direccion");
    }

    [Fact]
    public async Task ListarEquipos_Administrador_Responde403()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var admin = ClienteConRol("Administrador");

        var respuesta = await admin.GetAsync($"/api/sesiones/{sesionId}/equipos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListarEquipos_SesionInexistente_Responde404()
    {
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.GetAsync(
            $"/api/sesiones/{Guid.NewGuid()}/equipos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DetalleEquipo_Inexistente_Responde404()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.GetAsync(
            $"/api/sesiones/{sesionId}/equipos/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListarEquipos_SesionIndividual_Responde409()
    {
        var operador = ClienteConRol("Operador");
        var creada = await operador.PostAsJsonAsync("/api/sesiones", new CrearSesionSolicitudDto
        {
            Nombre = "Individual HU43",
            Descripcion = "Demo",
            Modo = "Individual",
            FechaProgramada = DateTime.UtcNow.AddHours(2),
            MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
            MaximoParticipantes = 10
        });
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var participante = ClienteConRol("Participante", IdParticipante);
        var respuesta = await participante.GetAsync(
            $"/api/sesiones/{creado!.Id}/equipos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- HU41: modificar equipo ----

    private static ModificarEquipoDto ModificarPublico(string nombre) => new()
    {
        Nombre = nombre,
        Tipo = TipoEquipoDto.Publico,
    };

    private static ModificarEquipoDto ModificarPrivado(string nombre, string? pass) => new()
    {
        Nombre = nombre,
        Tipo = TipoEquipoDto.Privado,
        Contrasena = pass,
    };

    [Fact]
    public async Task ModificarEquipo_Lider_Responde200YPersiste()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var respuesta = await participante.PutAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}", ModificarPublico("Verde"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content
            .ReadFromJsonAsync<ModificarEquipoRespuestaDto>(OpcionesJson);
        dto!.Nombre.Should().Be("Verde");

        var detalle = await (await participante.GetAsync(
                $"/api/sesiones/{sesionId}/equipos/{creado.Id}"))
            .Content.ReadFromJsonAsync<EquipoSesionDetalleDto>(OpcionesJson);
        detalle!.Nombre.Should().Be("Verde");
    }

    [Fact]
    public async Task ModificarEquipo_PrivadoNoPersisteTextoPlano()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var respuesta = await participante.PutAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}",
            ModificarPrivado("Rojo", "secretaXYZ"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        var equipo = await ctx.Equipos.FirstAsync(e => e.Id == creado.Id);
        equipo.Tipo.Should().Be(TipoEquipo.Privado);
        equipo.ContrasenaHash.Should().NotBeNullOrWhiteSpace();
        equipo.ContrasenaHash.Should().NotContain("secretaXYZ");
    }

    [Fact]
    public async Task ModificarEquipo_NoLider_Responde403()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var lider = ClienteConRol("Participante", IdParticipante);
        var creado = await (await lider.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var otro = ClienteConRol("Participante", Guid.NewGuid());
        var respuesta = await otro.PutAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}", ModificarPublico("Verde"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ModificarEquipo_SesionNoEnPreparacion_Responde409()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        await CambiarEstadoAsync(sesionId, EstadoSesion.Activa);

        var respuesta = await participante.PutAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}", ModificarPublico("Verde"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ModificarEquipo_NombreDuplicado_Responde409()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var p1 = ClienteConRol("Participante", IdParticipante);
        var creado = await (await p1.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Uno")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var p2 = ClienteConRol("Participante", Guid.NewGuid());
        (await p2.PostAsJsonAsync($"/api/sesiones/{sesionId}/equipos", EquipoPublico("Dos")))
            .EnsureSuccessStatusCode();

        var respuesta = await p1.PutAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}", ModificarPublico("Dos"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- RB: participación única en una sesión a la vez ----

    [Fact]
    public async Task CrearEquipo_ParticipanteEnOtraSesionActiva_Responde409()
    {
        var sesionA = await CrearSesionGrupalEnPreparacionAsync();
        var sesionB = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        (await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionA}/equipos", EquipoPublico("Rojo")))
            .EnsureSuccessStatusCode();

        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionB}/equipos", EquipoPublico("Azul"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CrearEquipo_SesionAnteriorFinalizada_Funciona()
    {
        var sesionA = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        (await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionA}/equipos", EquipoPublico("Rojo")))
            .EnsureSuccessStatusCode();

        // Al finalizar la sesión anterior, el participante queda libre.
        await CambiarEstadoAsync(sesionA, EstadoSesion.Finalizada);

        var sesionB = await CrearSesionGrupalEnPreparacionAsync();
        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionB}/equipos", EquipoPublico("Azul"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DetalleSesion_ConOtraSesionActiva_PuedeIngresarFalse()
    {
        var sesionA = await CrearSesionGrupalEnPreparacionAsync();
        var sesionB = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        (await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionA}/equipos", EquipoPublico("Rojo")))
            .EnsureSuccessStatusCode();

        var detalle = await (await participante.GetAsync(
                $"/api/sesiones/participante/disponibles/{sesionB}"))
            .Content.ReadFromJsonAsync<SesionDetalleMovilDto>(OpcionesJson);

        detalle!.PuedeIngresar.Should().BeFalse();
        detalle.MotivoNoPuedeIngresar.Should().NotBeNullOrWhiteSpace();
        detalle.SesionActualId.Should().Be(sesionA);
    }

    // ---- HU42: eliminar equipo ----

    [Fact]
    public async Task EliminarEquipo_Lider_Responde204_YEliminaEquipoYParticipantes()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var respuesta = await participante.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        (await ctx.Equipos.AnyAsync(e => e.Id == creado.Id)).Should().BeFalse();
        (await ctx.Participantes.AnyAsync(p => p.EquipoId == creado.Id)).Should().BeFalse();
        // La sesión y sus misiones siguen existiendo.
        (await ctx.Sesiones.AnyAsync(s => s.Id == sesionId)).Should().BeTrue();
        (await ctx.SesionMisiones.AnyAsync(m => m.SesionId == sesionId)).Should().BeTrue();
    }

    [Fact]
    public async Task EliminarEquipo_NoEliminaOtrosEquipos_NiApareceEnListado()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var p1 = ClienteConRol("Participante", IdParticipante);
        var rojo = await (await p1.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);
        var p2 = ClienteConRol("Participante", Guid.NewGuid());
        var azul = await (await p2.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Azul")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        (await p1.DeleteAsync($"/api/sesiones/{sesionId}/equipos/{rojo!.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var equipos = await (await p2.GetAsync($"/api/sesiones/{sesionId}/equipos"))
            .Content.ReadFromJsonAsync<List<EquipoSesionListadoDto>>(OpcionesJson);
        equipos.Should().ContainSingle(e => e.Id == azul!.Id);
        equipos.Should().NotContain(e => e.Id == rojo.Id);

        // El detalle del equipo eliminado responde 404.
        (await p1.GetAsync($"/api/sesiones/{sesionId}/equipos/{rojo.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EliminarEquipo_LiberaParticipacion_PermiteCrearDeNuevo()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        (await participante.DeleteAsync($"/api/sesiones/{sesionId}/equipos/{creado!.Id}"))
            .EnsureSuccessStatusCode();

        // Ya no queda participación activa: puede crear otro equipo en la sesión.
        var respuesta = await participante.PostAsJsonAsync(
            $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Verde"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task EliminarEquipo_NoLider_Responde403()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var lider = ClienteConRol("Participante", IdParticipante);
        var creado = await (await lider.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        var otro = ClienteConRol("Participante", Guid.NewGuid());
        var respuesta = await otro.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EliminarEquipo_SesionActiva_Responde409()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);
        var creado = await (await participante.PostAsJsonAsync(
                $"/api/sesiones/{sesionId}/equipos", EquipoPublico("Rojo")))
            .Content.ReadFromJsonAsync<CrearEquipoRespuestaDto>(OpcionesJson);

        await CambiarEstadoAsync(sesionId, EstadoSesion.Activa);

        var respuesta = await participante.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{creado!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EliminarEquipo_Inexistente_Responde404()
    {
        var sesionId = await CrearSesionGrupalEnPreparacionAsync();
        var participante = ClienteConRol("Participante", IdParticipante);

        var respuesta = await participante.DeleteAsync(
            $"/api/sesiones/{sesionId}/equipos/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
