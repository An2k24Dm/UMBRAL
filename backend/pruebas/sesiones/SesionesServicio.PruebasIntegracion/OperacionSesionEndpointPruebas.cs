using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// HU52 — Endpoints de operación del ciclo de vida (iniciar/pausar/reanudar/
// cancelar). Verifican el wiring HTTP (controlador → MediatR → fachada →
// dominio/State), la política solo-Operador, el mapeo de excepciones a
// estados HTTP y la persistencia del cambio de estado.
public class OperacionSesionEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FabricaApiPruebas _fabrica;

    public OperacionSesionEndpointPruebas(FabricaApiPruebas fabrica)
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

    // Siembra una sesión Individual en el estado indicado, con o sin inscrito.
    private Guid SembrarSesionIndividual(
        EstadoSesion estado, Guid operadorId, string codigo,
        bool conInscrito, DateTime? fechaProgramada = null)
    {
        var id = Guid.NewGuid();
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();

        var modelo = new SesionModelo
        {
            Id = id,
            TipoSesion = "Individual",
            Nombre = "Operación",
            Descripcion = "Demo",
            Estado = estado,
            FechaProgramada = fechaProgramada ?? DateTime.UtcNow.AddHours(-1),
            CodigoAcceso = codigo,
            OperadorCreadorId = operadorId,
            FechaCreacion = DateTime.UtcNow,
            MaximoParticipantes = 10
        };
        modelo.Misiones.Add(new SesionMisionModelo
        {
            Id = Guid.NewGuid(), SesionId = id,
            MisionId = FabricaApiPruebas.IdMisionActiva, Orden = 1
        });
        if (conInscrito)
        {
            modelo.Participantes.Add(new ParticipanteModelo
            {
                Id = Guid.NewGuid(), SesionId = id,
                ParticipanteIdentidadId = Guid.NewGuid(), EquipoId = null,
                Puntaje = 0, FechaUnionSesion = DateTime.UtcNow
            });
        }

        ctx.Sesiones.Add(modelo);
        ctx.SaveChanges();
        return id;
    }

    private string EstadoEnBd(Guid id)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        return ctx.Sesiones.Single(s => s.Id == id).Estado.ToString();
    }

    private static Task<HttpResponseMessage> Operar(HttpClient cliente, Guid id, string accion)
        => cliente.PatchAsync($"/api/sesiones/{id}/{accion}", content: null);

    [Fact]
    public async Task Iniciar_EnPreparacionConInscrito_Responde200YPersisteActiva()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.EnPreparacion, FabricaApiPruebas.IdOperadorPrueba, "OP-INI", conInscrito: true);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "iniciar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<OperacionSesionRespuestaDto>(OpcionesJson);
        dto!.Estado.Should().Be("Activa");
        dto.FechaInicioUtc.Should().NotBeNull();
        EstadoEnBd(id).Should().Be("Activa");
    }

    [Fact]
    public async Task Iniciar_SinInscritos_Responde409YNoCambia()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.EnPreparacion, FabricaApiPruebas.IdOperadorPrueba, "OP-SIN", conInscrito: false);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "iniciar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        EstadoEnBd(id).Should().Be("EnPreparacion");
    }

    [Fact]
    public async Task Pausar_SesionActiva_Responde200YPersistePausada()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Activa, FabricaApiPruebas.IdOperadorPrueba, "OP-PAU", conInscrito: true);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "pausar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        EstadoEnBd(id).Should().Be("Pausada");
    }

    [Fact]
    public async Task Reanudar_SesionPausada_Responde200YPersisteActiva()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Pausada, FabricaApiPruebas.IdOperadorPrueba, "OP-REA", conInscrito: true);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "reanudar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        EstadoEnBd(id).Should().Be("Activa");
    }

    [Fact]
    public async Task Cancelar_SesionActiva_Responde200YPersisteCancelada()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Activa, FabricaApiPruebas.IdOperadorPrueba, "OP-CAN", conInscrito: true);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "cancelar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        EstadoEnBd(id).Should().Be("Cancelada");
    }

    [Fact]
    public async Task Pausar_SesionEnPreparacion_Responde409()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.EnPreparacion, FabricaApiPruebas.IdOperadorPrueba, "OP-P409", conInscrito: true);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "pausar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        EstadoEnBd(id).Should().Be("EnPreparacion");
    }

    [Fact]
    public async Task Cancelar_SesionProgramada_Responde409()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Programada, FabricaApiPruebas.IdOperadorPrueba, "OP-CPRO", conInscrito: false);
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, id, "cancelar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        EstadoEnBd(id).Should().Be("Programada");
    }

    [Fact]
    public async Task Operar_SesionDeOtroOperador_Responde403()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Activa, FabricaApiPruebas.IdOperadorPrueba, "OP-OTRO", conInscrito: true);
        var clienteOtro = ClienteConRol("Operador", FabricaApiPruebas.IdOtroOperador);

        var respuesta = await Operar(clienteOtro, id, "pausar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        EstadoEnBd(id).Should().Be("Activa");
    }

    [Fact]
    public async Task Iniciar_SesionInexistente_Responde404()
    {
        var cliente = ClienteConRol("Operador", FabricaApiPruebas.IdOperadorPrueba);

        var respuesta = await Operar(cliente, Guid.NewGuid(), "iniciar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Participante")]
    public async Task Operar_RolNoOperador_Responde403(string rol)
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Activa, FabricaApiPruebas.IdOperadorPrueba, "OP-ROL-" + rol[..3], conInscrito: true);
        var cliente = ClienteConRol(rol);

        var respuesta = await Operar(cliente, id, "pausar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Operar_SinToken_Responde401()
    {
        var id = SembrarSesionIndividual(
            EstadoSesion.Activa, FabricaApiPruebas.IdOperadorPrueba, "OP-401", conInscrito: true);
        var cliente = _fabrica.CreateClient();

        var respuesta = await Operar(cliente, id, "pausar");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
