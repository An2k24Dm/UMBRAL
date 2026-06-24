using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

public class IngresarSesionEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly Guid ParticipanteId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly FabricaApiPruebas _fabrica;

    public IngresarSesionEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private HttpClient Cliente(string rol = "Participante", Guid? id = null)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
        cliente.DefaultRequestHeaders.Add(
            AuthHandlerPruebas.CabeceraIdKeycloak,
            (id ?? ParticipanteId).ToString());
        return cliente;
    }

    private Guid SembrarSesion(string tipo, string codigo, EstadoSesion estado)
    {
        var id = Guid.NewGuid();
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        contexto.Sesiones.Add(new SesionModelo
        {
            Id = id,
            TipoSesion = tipo,
            Nombre = $"Sesión {tipo}",
            Descripcion = "Demo",
            Estado = estado,
            FechaProgramada = DateTime.UtcNow.AddHours(1),
            CodigoAcceso = codigo,
            OperadorCreadorId = Guid.NewGuid(),
            FechaCreacion = DateTime.UtcNow,
            MaximoParticipantes = tipo == "Individual" ? 2 : null,
            MaximoEquipos = tipo == "Grupal" ? 3 : null,
            MaximoParticipantesPorEquipo = tipo == "Grupal" ? 2 : null
        });
        contexto.SaveChanges();
        return id;
    }

    [Fact]
    public async Task CodigoIndividual_RegistraParticipanteYPersiste()
    {
        var codigo = "IND" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var sesionId = SembrarSesion("Individual", codigo, EstadoSesion.EnPreparacion);

        var respuesta = await Cliente().PostAsJsonAsync(
            "/api/sesiones/participante/ingresar",
            new IngresarSesionDto { CodigoSesion = codigo.ToLowerInvariant() });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<IngresarSesionRespuestaDto>(Json);
        dto!.IngresoRegistrado.Should().BeTrue();
        dto.Modo.Should().Be("Individual");
        dto.ParticipacionActual!.EstaInscrito.Should().BeTrue();

        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        contexto.Participantes.Should().ContainSingle(p =>
            p.SesionId == sesionId &&
            p.ParticipanteIdentidadId == ParticipanteId &&
            p.EquipoId == null);
    }

    [Fact]
    public async Task CodigoGrupal_RedirigeSinCrearParticipanteNiEquipo()
    {
        var codigo = "GRP" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var sesionId = SembrarSesion("Grupal", codigo, EstadoSesion.EnPreparacion);

        var respuesta = await Cliente().PostAsJsonAsync(
            "/api/sesiones/participante/ingresar",
            new IngresarSesionDto { CodigoSesion = $"  {codigo.ToLowerInvariant()}  " });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<IngresarSesionRespuestaDto>(Json);
        dto!.IngresoRegistrado.Should().BeFalse();
        dto.RedirigirADetalle.Should().BeTrue();
        dto.Modo.Should().Be("Grupal");
        dto.RequiereEquipo.Should().BeTrue();
        dto.ParticipacionActual!.EstaInscrito.Should().BeFalse();

        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
        contexto.Equipos.Should().NotContain(e => e.SesionId == sesionId);
        contexto.Participantes.Should().NotContain(p => p.SesionId == sesionId);
        contexto.Participantes.Should().NotContain(p =>
            p.SesionId == sesionId && p.EquipoId == null);
    }

    [Fact]
    public async Task DetalleIndividual_Unirse_RegistraParticipante()
    {
        var codigo = "DET" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var sesionId = SembrarSesion("Individual", codigo, EstadoSesion.EnPreparacion);
        var participante = Guid.NewGuid();

        var respuesta = await Cliente(id: participante).PostAsync(
            $"/api/sesiones/{sesionId}/participante/ingresar-individual", null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<IngresarSesionRespuestaDto>(Json);
        dto!.IngresoRegistrado.Should().BeTrue();
        dto.SesionId.Should().Be(sesionId);
    }

    [Fact]
    public async Task DetalleIndividual_RechazaSesionGrupal()
    {
        var sesionId = SembrarSesion(
            "Grupal", "REJ" + Guid.NewGuid().ToString("N")[..6],
            EstadoSesion.EnPreparacion);

        var respuesta = await Cliente().PostAsync(
            $"/api/sesiones/{sesionId}/participante/ingresar-individual", null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CodigoInexistente_Responde404()
    {
        var respuesta = await Cliente().PostAsJsonAsync(
            "/api/sesiones/participante/ingresar",
            new IngresarSesionDto { CodigoSesion = "NO-EXISTE" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UsuarioNoParticipante_Responde403()
    {
        var respuesta = await Cliente("Operador").PostAsJsonAsync(
            "/api/sesiones/participante/ingresar",
            new IngresarSesionDto { CodigoSesion = "CUALQUIERA" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
