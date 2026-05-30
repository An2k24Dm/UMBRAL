using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

// HU33 — Pruebas del filtro "existe sesión vigente" del repositorio
// contra una base InMemory. Cubrimos los seis estados del enum y
// además los casos de filtrado por contenido y por TipoJuego.
public class RepositorioSesionesExisteSesionVigentePruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ContenidoConsultado = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtroContenido = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid CreadorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ContextoSesiones CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase($"sesiones-{Guid.NewGuid()}")
            .Options;
        return new ContextoSesiones(opciones);
    }

    private static async Task SembrarAsync(
        ContextoSesiones contexto, TipoJuego tipoJuego, Guid contenidoJuegoId, EstadoSesion estado)
    {
        var sesion = Sesion.Rehidratar(
            id: Guid.NewGuid(),
            nombre: $"Sesión {estado}",
            tipoJuego: tipoJuego,
            contenidoJuegoId: contenidoJuegoId,
            modo: ModoSesion.Individual,
            estado: estado,
            fechaProgramada: AhoraUtc.AddHours(2),
            creadaPorUsuarioId: CreadorId,
            fechaCreacion: AhoraUtc);

        contexto.Sesiones.Add(SesionesMapeador.HaciaModelo(sesion));
        await contexto.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<bool> EvaluarAsync(
        Action<ContextoSesiones> sembrar, TipoJuego tipoConsulta, Guid idConsulta)
    {
        await using var contexto = CrearContexto();
        sembrar(contexto);
        await contexto.SaveChangesAsync(CancellationToken.None);

        var repositorio = new RepositorioSesiones(contexto);
        return await repositorio.ExisteSesionVigentePorContenidoAsync(
            tipoConsulta, idConsulta, CancellationToken.None);
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task DevuelveTrue_CuandoExisteSesionEnEstadoVigente(EstadoSesion estado)
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, TipoJuego.Trivia, ContenidoConsultado, estado);

        var repositorio = new RepositorioSesiones(contexto);
        var existe = await repositorio.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoConsultado, CancellationToken.None);

        existe.Should().BeTrue();
    }

    [Theory]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task DevuelveFalse_CuandoSoloHaySesionesEnEstadoNoVigente(EstadoSesion estado)
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, TipoJuego.Trivia, ContenidoConsultado, estado);

        var repositorio = new RepositorioSesiones(contexto);
        var existe = await repositorio.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoConsultado, CancellationToken.None);

        existe.Should().BeFalse();
    }

    [Fact]
    public async Task DevuelveFalse_CuandoNoHaySesionesParaEseContenido()
    {
        await using var contexto = CrearContexto();
        // No sembramos nada.
        var repositorio = new RepositorioSesiones(contexto);

        var existe = await repositorio.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoConsultado, CancellationToken.None);

        existe.Should().BeFalse();
    }

    [Fact]
    public async Task DevuelveFalse_CuandoLasSesionesVigentesSonDeOtroContenido()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, TipoJuego.Trivia, OtroContenido, EstadoSesion.Activa);

        var repositorio = new RepositorioSesiones(contexto);
        var existe = await repositorio.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoConsultado, CancellationToken.None);

        existe.Should().BeFalse();
    }

    [Fact]
    public async Task DevuelveFalse_CuandoMismoIdPeroOtroTipoJuego()
    {
        await using var contexto = CrearContexto();
        // Mismo ContenidoJuegoId, pero TipoJuego BusquedaTesoro.
        await SembrarAsync(contexto, TipoJuego.BusquedaTesoro, ContenidoConsultado, EstadoSesion.Programada);

        var repositorio = new RepositorioSesiones(contexto);
        var existe = await repositorio.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoConsultado, CancellationToken.None);

        existe.Should().BeFalse();
    }

    [Fact]
    public async Task DevuelveTrue_CuandoCoexistenSesionesFinalizadasYUnaVigente()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, TipoJuego.Trivia, ContenidoConsultado, EstadoSesion.Finalizada);
        await SembrarAsync(contexto, TipoJuego.Trivia, ContenidoConsultado, EstadoSesion.Cancelada);
        await SembrarAsync(contexto, TipoJuego.Trivia, ContenidoConsultado, EstadoSesion.Pausada);

        var repositorio = new RepositorioSesiones(contexto);
        var existe = await repositorio.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoConsultado, CancellationToken.None);

        existe.Should().BeTrue();
    }
}
