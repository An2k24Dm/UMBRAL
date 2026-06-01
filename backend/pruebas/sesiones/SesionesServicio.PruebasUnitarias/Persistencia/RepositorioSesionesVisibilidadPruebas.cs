using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

// HU34 — Verifica el listado base (con filtros) y las consultas usadas
// por el HostedService de preparación. El repositorio NO conoce el rol
// del creador: la regla de visibilidad por rol se aplica en Aplicación.
public class RepositorioSesionesVisibilidadPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OperadorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OperadorB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ContextoSesiones CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase($"vis-{Guid.NewGuid()}")
            .Options;
        return new ContextoSesiones(opciones);
    }

    private static async Task SembrarAsync(
        ContextoSesiones contexto,
        Guid creadorId,
        TipoJuego tipoJuego = TipoJuego.Trivia,
        EstadoSesion estado = EstadoSesion.Programada,
        DateTime? fechaProgramada = null)
    {
        var sesion = Sesion.Rehidratar(
            Guid.NewGuid(),
            $"Sesión {creadorId.ToString()[..4]}",
            tipoJuego,
            Guid.NewGuid(),
            ModoSesion.Individual,
            estado,
            fechaProgramada ?? AhoraUtc.AddHours(1),
            creadorId,
            AhoraUtc);

        contexto.Sesiones.Add(SesionesMapeador.HaciaModelo(sesion));
        await contexto.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ListarAsync_SinFiltros_DevuelveTodas()
    {
        await using var ctx = CrearContexto();
        await SembrarAsync(ctx, Admin);
        await SembrarAsync(ctx, OperadorA);
        await SembrarAsync(ctx, OperadorB);

        var repo = new RepositorioSesiones(ctx);
        var lista = await repo.ListarAsync(null, null, CancellationToken.None);

        lista.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListarAsync_FiltraPorTipoJuego()
    {
        await using var ctx = CrearContexto();
        await SembrarAsync(ctx, OperadorA, TipoJuego.Trivia);
        await SembrarAsync(ctx, OperadorA, TipoJuego.BusquedaTesoro);
        await SembrarAsync(ctx, Admin, TipoJuego.BusquedaTesoro);

        var repo = new RepositorioSesiones(ctx);
        var lista = await repo.ListarAsync(
            TipoJuego.BusquedaTesoro, null, CancellationToken.None);

        lista.Should().HaveCount(2);
        lista.Should().OnlyContain(s => s.TipoJuego == TipoJuego.BusquedaTesoro);
    }

    [Fact]
    public async Task ListarAsync_FiltraPorEstado()
    {
        await using var ctx = CrearContexto();
        await SembrarAsync(ctx, Admin, estado: EstadoSesion.Programada);
        await SembrarAsync(ctx, Admin, estado: EstadoSesion.Activa);
        await SembrarAsync(ctx, Admin, estado: EstadoSesion.Finalizada);

        var repo = new RepositorioSesiones(ctx);
        var lista = await repo.ListarAsync(
            null, EstadoSesion.Activa, CancellationToken.None);

        lista.Should().ContainSingle();
        lista[0].Estado.Should().Be(EstadoSesion.Activa);
    }

    [Fact]
    public async Task ListarProgramadasVencidas_DevuelveSoloProgramadasYConFechaPasada()
    {
        await using var ctx = CrearContexto();
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.Programada,
            fechaProgramada: AhoraUtc.AddMinutes(-5));
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.Programada,
            fechaProgramada: AhoraUtc.AddMinutes(15));
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.EnPreparacion,
            fechaProgramada: AhoraUtc.AddMinutes(-30));
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.Activa,
            fechaProgramada: AhoraUtc.AddMinutes(-30));
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.Finalizada,
            fechaProgramada: AhoraUtc.AddMinutes(-30));
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.Cancelada,
            fechaProgramada: AhoraUtc.AddMinutes(-30));

        var repo = new RepositorioSesiones(ctx);
        var lista = await repo.ListarProgramadasVencidasAsync(AhoraUtc, CancellationToken.None);

        lista.Should().ContainSingle();
        lista[0].Estado.Should().Be(EstadoSesion.Programada);
        lista[0].FechaProgramada.Should().BeBefore(AhoraUtc);
    }

    [Fact]
    public async Task ActualizarAsync_DebePersistirCambioDeEstado()
    {
        await using var ctx = CrearContexto();
        await SembrarAsync(ctx, Admin,
            estado: EstadoSesion.Programada,
            fechaProgramada: AhoraUtc.AddMinutes(-1));

        var repo = new RepositorioSesiones(ctx);
        var sesion = (await repo.ListarProgramadasVencidasAsync(AhoraUtc, CancellationToken.None))[0];

        sesion.Preparar();
        await repo.ActualizarAsync(sesion, CancellationToken.None);
        await ctx.SaveChangesAsync();

        var recargada = await repo.ObtenerPorIdAsync(sesion.Id, CancellationToken.None);
        recargada!.Estado.Should().Be(EstadoSesion.EnPreparacion);
    }
}
