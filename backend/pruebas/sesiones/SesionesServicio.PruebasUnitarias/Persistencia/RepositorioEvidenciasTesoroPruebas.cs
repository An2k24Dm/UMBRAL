using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

// Cubre la lógica de conteo por "jugador" (participante individual o equipo) y
// la existencia de evidencia válida del repositorio de evidencias del Tesoro.
// Usa EF Core InMemory. Nota: InMemory NO aplica los índices únicos filtrados;
// la barrera real ante la carrera (dos integrantes simultáneos) la impone
// PostgreSQL con esos índices (ver migración CorregirEvidenciaTesoroPorEquipo)
// y su traducción a EvidenciaTesoroDuplicadaExcepcion se cubre en las pruebas
// del manejador.
public class RepositorioEvidenciasTesoroPruebas
{
    private static readonly Guid SesionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid BusquedaId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly Guid Ana = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid Pedro = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
    private static readonly Guid Carlos = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");

    private static readonly Guid EquipoRojo = Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4");
    private static readonly Guid EquipoAzul = Guid.Parse("e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5");

    private static ContextoSesiones NuevoContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase("evidencias-" + Guid.NewGuid())
            .Options;
        return new ContextoSesiones(opciones);
    }

    private static EvidenciaTesoroRegistro Registro(Guid participante, Guid? equipo, bool valida)
        => new(
            SesionId: SesionId,
            MisionId: MisionId,
            EtapaId: EtapaId,
            BusquedaId: BusquedaId,
            ParticipanteIdentidadId: participante,
            EquipoId: equipo,
            CodigoEnviado: "QR",
            EsValida: valida,
            PuntosGanados: valida ? 50 : 0,
            FechaEnvioUtc: DateTime.UtcNow);

    private static async Task SembrarAsync(
        IRepositorioEvidenciasTesoro repo, params EvidenciaTesoroRegistro[] registros)
    {
        foreach (var r in registros)
            await repo.AgregarAsync(r, CancellationToken.None);
    }

    [Fact] // (3) Dos equipos distintos con evidencia válida.
    public async Task ContarEquipos_DosEquiposDistintos_Cuenta2()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioEvidenciasTesoro(ctx);
        await SembrarAsync(repo,
            Registro(Ana, EquipoRojo, valida: true),
            Registro(Carlos, EquipoAzul, valida: true));

        var equipos = await repo.ContarEquiposConEvidenciaValidaAsync(
            SesionId, EtapaId, CancellationToken.None);

        equipos.Should().Be(2);
    }

    [Fact] // (4, 8) Dos integrantes del mismo equipo con válida cuentan como UN equipo.
    public async Task ContarEquipos_DosIntegrantesMismoEquipo_Cuenta1()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioEvidenciasTesoro(ctx);
        await SembrarAsync(repo,
            Registro(Ana, EquipoRojo, valida: true),
            Registro(Pedro, EquipoRojo, valida: true));

        var equipos = await repo.ContarEquiposConEvidenciaValidaAsync(
            SesionId, EtapaId, CancellationToken.None);

        equipos.Should().Be(1);
    }

    [Fact] // (5) Dos participantes individuales distintos completan independientemente.
    public async Task ContarParticipantes_DosIndividualesDistintos_Cuenta2()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioEvidenciasTesoro(ctx);
        await SembrarAsync(repo,
            Registro(Ana, equipo: null, valida: true),
            Registro(Carlos, equipo: null, valida: true));

        var participantes = await repo.ContarParticipantesConEvidenciaValidaAsync(
            SesionId, EtapaId, CancellationToken.None);

        participantes.Should().Be(2);
    }

    [Fact] // El conteo individual ignora evidencias grupales (equipo_id IS NOT NULL).
    public async Task ContarParticipantes_IgnoraEvidenciasGrupales()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioEvidenciasTesoro(ctx);
        await SembrarAsync(repo,
            Registro(Ana, equipo: null, valida: true),      // individual
            Registro(Pedro, EquipoRojo, valida: true));     // grupal

        var participantes = await repo.ContarParticipantesConEvidenciaValidaAsync(
            SesionId, EtapaId, CancellationToken.None);
        var equipos = await repo.ContarEquiposConEvidenciaValidaAsync(
            SesionId, EtapaId, CancellationToken.None);

        participantes.Should().Be(1);
        equipos.Should().Be(1);
    }

    [Fact] // La evidencia válida del equipo la detecta cualquier integrante.
    public async Task ExisteEvidenciaValidaEquipo_EsPorEquipo()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioEvidenciasTesoro(ctx);
        await SembrarAsync(repo, Registro(Ana, EquipoRojo, valida: true));

        (await repo.ExisteEvidenciaValidaEquipoAsync(
            SesionId, EtapaId, EquipoRojo, CancellationToken.None)).Should().BeTrue();
        (await repo.ExisteEvidenciaValidaEquipoAsync(
            SesionId, EtapaId, EquipoAzul, CancellationToken.None)).Should().BeFalse();
    }

    [Fact] // (6) Un QR inválido no marca como completado y permite un intento válido posterior.
    public async Task QrInvalido_NoMarcaValida_PermiteReintentoValido()
    {
        using var ctx = NuevoContexto();
        var repo = new RepositorioEvidenciasTesoro(ctx);

        // Primer intento inválido.
        await SembrarAsync(repo, Registro(Ana, equipo: null, valida: false));

        (await repo.ExisteEvidenciaValidaIndividualAsync(
            SesionId, EtapaId, Ana, CancellationToken.None)).Should().BeFalse();

        // Segundo intento válido.
        await SembrarAsync(repo, Registro(Ana, equipo: null, valida: true));

        (await repo.ExisteEvidenciaValidaIndividualAsync(
            SesionId, EtapaId, Ana, CancellationToken.None)).Should().BeTrue();
        (await repo.ContarParticipantesConEvidenciaValidaAsync(
            SesionId, EtapaId, CancellationToken.None)).Should().Be(1);
    }
}
