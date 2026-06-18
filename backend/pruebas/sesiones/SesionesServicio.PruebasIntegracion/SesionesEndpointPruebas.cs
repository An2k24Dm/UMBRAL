using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

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
        MisionesIds = misiones ?? new List<Guid> { FabricaApiPruebas.IdMisionActiva },
        MaximoParticipantes = 15
    };

    private static CrearSesionSolicitudDto DtoGrupalValido(List<Guid>? misiones = null) => new()
    {
        Nombre = "Sesión grupal de integración",
        Descripcion = "Demo",
        Modo = "Grupal",
        FechaProgramada = DateTime.UtcNow.AddHours(2),
        MisionesIds = misiones ?? new List<Guid> { FabricaApiPruebas.IdMisionActiva },
        MaximoEquipos = 6,
        MaximoParticipantesPorEquipo = 3
    };

    private static ModificarSesionDto ModificarIndividual(
        int maxParticipantes = 30, List<Guid>? misiones = null) => new()
    {
        Nombre = "Sesión editada",
        Descripcion = "Descripción editada",
        Modo = "Individual",
        FechaProgramada = DateTime.UtcNow.AddHours(4),
        MisionesIds = misiones ?? new List<Guid> { FabricaApiPruebas.IdMisionActivaB },
        MaximoParticipantes = maxParticipantes
    };

    [Fact]
    public async Task Operador_ModificaSuSesion_Responde200YReflejaCambios()
    {
        var cliente = ClienteConRol("Operador");
        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/sesiones/{creado!.Id}", ModificarIndividual(maxParticipantes: 30));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<SesionDetalleDto>(OpcionesJson);
        detalle!.Nombre.Should().Be("Sesión editada");
        detalle.Descripcion.Should().Be("Descripción editada");
        detalle.MaximoParticipantes.Should().Be(30);
        detalle.Misiones.Select(m => m.MisionId)
            .Should().Equal(new[] { FabricaApiPruebas.IdMisionActivaB });
        // Código de acceso y estado no cambian.
        detalle.CodigoAcceso.Should().Be(FabricaApiPruebas.CodigoAccesoPrueba);
        detalle.Estado.Should().Be("Programada");
    }

    [Fact]
    public async Task Modificar_SeRefleja_EnDetalleYListado()
    {
        var cliente = ClienteConRol("Operador");
        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var modificar = ModificarIndividual(maxParticipantes: 42);
        modificar.Nombre = "Nombre visible";
        (await cliente.PutAsJsonAsync($"/api/sesiones/{creado!.Id}", modificar))
            .EnsureSuccessStatusCode();

        // Detalle
        var detalle = await (await cliente.GetAsync($"/api/sesiones/{creado.Id}"))
            .Content.ReadFromJsonAsync<SesionDetalleDto>(OpcionesJson);
        detalle!.Nombre.Should().Be("Nombre visible");
        detalle.MaximoParticipantes.Should().Be(42);

        // Listado
        var listado = await (await cliente.GetAsync("/api/sesiones"))
            .Content.ReadFromJsonAsync<List<SesionListadoDto>>(OpcionesJson);
        var enListado = listado!.Single(s => s.Id == creado.Id);
        enListado.Nombre.Should().Be("Nombre visible");
        enListado.MaximoParticipantes.Should().Be(42);
    }

    [Fact]
    public async Task Modificar_CambiaTipo_AGrupal_SinInscritos_Responde200()
    {
        var cliente = ClienteConRol("Operador");
        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var dto = new ModificarSesionDto
        {
            Nombre = "Ahora grupal",
            Descripcion = "Demo",
            Modo = "Grupal",
            FechaProgramada = DateTime.UtcNow.AddHours(4),
            MisionesIds = new List<Guid> { FabricaApiPruebas.IdMisionActiva },
            MaximoEquipos = 4,
            MaximoParticipantesPorEquipo = 2
        };

        var respuesta = await cliente.PutAsJsonAsync($"/api/sesiones/{creado!.Id}", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<SesionDetalleDto>(OpcionesJson);
        detalle!.Modo.Should().Be("Grupal");
        detalle.Id.Should().Be(creado.Id);
        detalle.CodigoAcceso.Should().Be(FabricaApiPruebas.CodigoAccesoPrueba);
        detalle.Estado.Should().Be("Programada");
        detalle.MaximoEquipos.Should().Be(4);
    }

    [Fact]
    public async Task Modificar_SesionInexistente_Responde404()
    {
        var cliente = ClienteConRol("Operador");

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/sesiones/{Guid.NewGuid()}", ModificarIndividual());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_SesionDeOtroOperador_Responde403()
    {
        var clienteA = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await clienteA.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var clienteB = ClienteConRol("Operador", FabricaApiPruebas.IdOtroOperador);
        var respuesta = await clienteB.PutAsJsonAsync(
            $"/api/sesiones/{creado!.Id}", ModificarIndividual());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Modificar_ComoAdministrador_Responde403()
    {
        // El Administrador es de solo lectura: no puede modificar sesiones.
        var operador = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await operador.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var admin = ClienteConRol("Administrador");
        var respuesta = await admin.PutAsJsonAsync(
            $"/api/sesiones/{creado!.Id}", ModificarIndividual());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Modificar_ComoParticipante_Responde403()
    {
        var operador = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await operador.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var participante = ClienteConRol("Participante");
        var respuesta = await participante.PutAsJsonAsync(
            $"/api/sesiones/{creado!.Id}", ModificarIndividual());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrador_PuedeConsultarDetalle()
    {
        var operador = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await operador.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var admin = ClienteConRol("Administrador");
        var respuesta = await admin.GetAsync($"/api/sesiones/{creado!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Modificar_CapacidadInvalida_Responde400()
    {
        var cliente = ClienteConRol("Operador");
        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var dto = ModificarIndividual();
        dto.MaximoParticipantes = 0;

        var respuesta = await cliente.PutAsJsonAsync($"/api/sesiones/{creado!.Id}", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

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
    public async Task Operador_CreaSesionGrupal_Responde201()
    {
        var cliente = ClienteConRol("Operador");

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", DtoGrupalValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CrearIndividual_PersisteCapacidad_YSeConsultaEnDetalle()
    {
        var cliente = ClienteConRol("Operador");

        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var detalleResp = await cliente.GetAsync($"/api/sesiones/{creado!.Id}");
        detalleResp.EnsureSuccessStatusCode();
        var detalle = await detalleResp.Content.ReadFromJsonAsync<SesionDetalleDto>(OpcionesJson);

        detalle!.Modo.Should().Be("Individual");
        detalle.MaximoParticipantes.Should().Be(15);
        detalle.MaximoEquipos.Should().BeNull();
        detalle.MaximoParticipantesPorEquipo.Should().BeNull();
    }

    [Fact]
    public async Task CrearGrupal_PersisteCapacidades_YSeConsultanEnDetalle()
    {
        var cliente = ClienteConRol("Operador");

        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoGrupalValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var detalleResp = await cliente.GetAsync($"/api/sesiones/{creado!.Id}");
        detalleResp.EnsureSuccessStatusCode();
        var detalle = await detalleResp.Content.ReadFromJsonAsync<SesionDetalleDto>(OpcionesJson);

        detalle!.Modo.Should().Be("Grupal");
        detalle.MaximoEquipos.Should().Be(6);
        detalle.MaximoParticipantesPorEquipo.Should().Be(3);
        detalle.MaximoParticipantes.Should().BeNull();
    }

    [Fact]
    public async Task CrearIndividual_SinCapacidad_Responde400()
    {
        var cliente = ClienteConRol("Operador");
        var dto = DtoValido();
        dto.MaximoParticipantes = null;

        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones", dto);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    // -----------------------------------------------------------------------
    // HU39 — Eliminar sesión
    // -----------------------------------------------------------------------

    // Inserta directamente en la BD local una sesión con el estado y las filas
    // hijas indicadas, para cubrir escenarios que la API no permite alcanzar
    // (estados distintos de Programada, hijos en una sesión Programada).
    private void SembrarSesion(
        Guid id, EstadoSesion estado, Guid operadorId, string codigo,
        bool conHijos = false)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();

        var modelo = new SesionModelo
        {
            Id = id,
            TipoSesion = "Individual",
            Nombre = "Sembrada",
            Descripcion = "Demo",
            Estado = estado,
            FechaProgramada = DateTime.UtcNow.AddHours(2),
            CodigoAcceso = codigo,
            OperadorCreadorId = operadorId,
            FechaCreacion = DateTime.UtcNow,
            MaximoParticipantes = 10
        };

        if (conHijos)
        {
            var equipoId = Guid.NewGuid();
            modelo.Misiones.Add(new SesionMisionModelo
            {
                Id = Guid.NewGuid(), SesionId = id,
                MisionId = FabricaApiPruebas.IdMisionActiva, Orden = 1
            });
            modelo.Equipos.Add(new EquipoModelo
            {
                Id = equipoId, SesionId = id, Nombre = "Equipo Rojo",
                LiderParticipanteId = Guid.NewGuid(), Puntaje = 0,
                FechaCreacion = DateTime.UtcNow
            });
            modelo.Participantes.Add(new ParticipanteModelo
            {
                Id = Guid.NewGuid(), SesionId = id,
                ParticipanteIdentidadId = Guid.NewGuid(), EquipoId = equipoId,
                Puntaje = 0, FechaUnionSesion = DateTime.UtcNow
            });
        }

        ctx.Sesiones.Add(modelo);
        ctx.SaveChanges();
    }

    private (int sesiones, int misiones, int equipos, int participantes) ContarPorSesion(Guid id)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        return (
            ctx.Sesiones.Count(s => s.Id == id),
            ctx.SesionMisiones.Count(m => m.SesionId == id),
            ctx.Equipos.Count(e => e.SesionId == id),
            ctx.Participantes.Count(p => p.SesionId == id)
        );
    }

    [Fact]
    public async Task Operador_EliminaSesionProgramada_Responde204_YDesaparece()
    {
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var creada = await cliente.PostAsJsonAsync("/api/sesiones", DtoValido());
        creada.EnsureSuccessStatusCode();
        var creado = await creada.Content.ReadFromJsonAsync<CrearSesionRespuestaDto>(OpcionesJson);

        var eliminar = await cliente.DeleteAsync($"/api/sesiones/{creado!.Id}");
        eliminar.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // El detalle ya no existe.
        var detalle = await cliente.GetAsync($"/api/sesiones/{creado.Id}");
        detalle.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // El listado ya no la incluye.
        var listado = await (await cliente.GetAsync("/api/sesiones"))
            .Content.ReadFromJsonAsync<List<SesionListadoDto>>(OpcionesJson);
        listado!.Should().NotContain(s => s.Id == creado.Id);
    }

    [Fact]
    public async Task Eliminar_BorraFilasLocales_SesionMisionEquipoParticipante()
    {
        var id = Guid.NewGuid();
        SembrarSesion(id, EstadoSesion.Programada, FabricaApiPruebas.IdOperadorPrueba,
            "DEL-CAS", conHijos: true);

        // Precondición: existen las filas hijas.
        ContarPorSesion(id).Should().Be((1, 1, 1, 1));

        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var eliminar = await cliente.DeleteAsync($"/api/sesiones/{id}");
        eliminar.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // No quedan referencias locales con ese sesion_id.
        ContarPorSesion(id).Should().Be((0, 0, 0, 0));
    }

    [Fact]
    public async Task Eliminar_SesionEnPreparacion_Responde409()
    {
        var id = Guid.NewGuid();
        SembrarSesion(id, EstadoSesion.EnPreparacion, FabricaApiPruebas.IdOperadorPrueba, "DEL-ENP");

        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var eliminar = await cliente.DeleteAsync($"/api/sesiones/{id}");

        eliminar.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ContarPorSesion(id).sesiones.Should().Be(1); // no se borró
    }

    [Fact]
    public async Task Eliminar_SesionDeOtroOperador_Responde403()
    {
        var id = Guid.NewGuid();
        SembrarSesion(id, EstadoSesion.Programada, FabricaApiPruebas.IdOperadorPrueba, "DEL-OTRO");

        var clienteB = ClienteConRol("Operador", FabricaApiPruebas.IdOtroOperador);
        var eliminar = await clienteB.DeleteAsync($"/api/sesiones/{id}");

        eliminar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ContarPorSesion(id).sesiones.Should().Be(1);
    }

    [Fact]
    public async Task Eliminar_ComoAdministrador_Responde403()
    {
        var id = Guid.NewGuid();
        SembrarSesion(id, EstadoSesion.Programada, FabricaApiPruebas.IdOperadorPrueba, "DEL-ADM");

        var admin = ClienteConRol("Administrador");
        var eliminar = await admin.DeleteAsync($"/api/sesiones/{id}");

        eliminar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ContarPorSesion(id).sesiones.Should().Be(1);
    }

    [Fact]
    public async Task Eliminar_ComoParticipante_Responde403()
    {
        var id = Guid.NewGuid();
        SembrarSesion(id, EstadoSesion.Programada, FabricaApiPruebas.IdOperadorPrueba, "DEL-PAR");

        var participante = ClienteConRol("Participante");
        var eliminar = await participante.DeleteAsync($"/api/sesiones/{id}");

        eliminar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ContarPorSesion(id).sesiones.Should().Be(1);
    }

    [Fact]
    public async Task Eliminar_SesionInexistente_Responde404()
    {
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);
        var eliminar = await cliente.DeleteAsync($"/api/sesiones/{Guid.NewGuid()}");
        eliminar.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
