using RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

namespace RankingServicio.PruebasIntegracion;

// Endpoints de consulta de ranking por sesión (camino exitoso y sesión sin datos).
public sealed class RankingConsultasEndpointPruebas
    : IClassFixture<FabricaApiRankingPruebas>
{
    private readonly FabricaApiRankingPruebas _fabrica;

    public RankingConsultasEndpointPruebas(FabricaApiRankingPruebas fabrica)
        => _fabrica = fabrica;

    private HttpClient ClienteConRol(string rol)
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(AuthHandlerPruebas.CabeceraRol, rol);
        return cliente;
    }

    [Fact]
    public async Task Participantes_ConDatos_DevuelveListaOrdenadaYEnriquecida()
    {
        var cliente = ClienteConRol("Participante");

        var respuesta = await cliente.GetAsync(
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionConDatos}/participantes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<RankingParticipanteDto>>();
        lista.Should().NotBeNull();
        var r = lista!;
        r.Should().HaveCount(4);
        // Orden por puntaje descendente: A(100), B(50), D(40), C(30).
        r.Select(p => p.Puntaje).Should().ContainInOrder(100L, 50L, 40L, 30L);
        r[0].Posicion.Should().Be(1);
        r[0].Alias.Should().Be(FabricaApiRankingPruebas.AliasA); // enriquecido por identidad
        // B no fue enriquecido -> ResolucionAlias cae al identificador.
        r[1].Alias.Should().Be(r[1].ParticipanteIdentidadId.ToString());
    }

    [Fact]
    public async Task Participantes_SesionSinRanking_DevuelveListaVacia()
    {
        var cliente = ClienteConRol("Participante");

        var respuesta = await cliente.GetAsync(
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionSinRanking}/participantes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<RankingParticipanteDto>>();
        lista.Should().NotBeNull();
        lista!.Should().BeEmpty();
    }

    [Fact]
    public async Task Equipos_ConDatos_DevuelveEquiposOrdenadosConNombreYFallback()
    {
        var cliente = ClienteConRol("Operador");

        var respuesta = await cliente.GetAsync(
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionConDatos}/equipos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<RankingEquipoDto>>();
        lista.Should().NotBeNull();
        var r = lista!;
        r.Should().HaveCount(2);

        // Equipo1 = A(100)+B(50) = 150 ; Equipo2 = D(40).
        r[0].EquipoId.Should().Be(FabricaApiRankingPruebas.Equipo1);
        r[0].Puntaje.Should().Be(150L);
        r[0].NombreEquipo.Should().Be(FabricaApiRankingPruebas.NombreEquipo1); // enriquecido
        r[0].Participantes.Should().HaveCount(2);

        r[1].EquipoId.Should().Be(FabricaApiRankingPruebas.Equipo2);
        r[1].Puntaje.Should().Be(40L);
        // Equipo2 no fue enriquecido -> nombre cae al identificador.
        r[1].NombreEquipo.Should().Be(FabricaApiRankingPruebas.Equipo2.ToString());
    }

    [Fact]
    public async Task Equipos_SesionSinRanking_DevuelveListaVacia()
    {
        var cliente = ClienteConRol("Participante");

        var respuesta = await cliente.GetAsync(
            $"/api/ranking/sesiones/{FabricaApiRankingPruebas.SesionSinRanking}/equipos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await respuesta.Content.ReadFromJsonAsync<List<RankingEquipoDto>>();
        lista.Should().NotBeNull();
        lista!.Should().BeEmpty();
    }

    [Fact]
    public async Task Salud_DevuelveOkYNombreDelServicio()
    {
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().Contain("ranking-servicio");
    }
}
