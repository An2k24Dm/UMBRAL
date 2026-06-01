using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.CasosDeUso.Manejadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU34 — Visibilidad por rol y filtros opcionales en el listado.
// El rol del creador NO vive en la sesión: se consulta a
// identidad-servicio mediante IClienteIdentidadUsuarios.
public class ListarSesionesManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OperadorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OperadorB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Admin = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Admin2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static Sesion Sesion(
        Guid creadorId,
        TipoJuego tipoJuego = TipoJuego.Trivia,
        EstadoSesion estado = EstadoSesion.Programada)
        => SesionesServicio.Dominio.Entidades.Sesion.Rehidratar(
            Guid.NewGuid(),
            "Sesión",
            tipoJuego,
            Guid.NewGuid(),
            ModoSesion.Individual,
            estado,
            AhoraUtc.AddHours(1),
            creadorId,
            AhoraUtc);

    private static (ListarSesionesManejador manejador,
                    Mock<IRepositorioSesiones> repo,
                    Mock<IClienteIdentidadUsuarios> identidad)
        Crear(string[] roles, Guid? usuarioId = null, bool autenticado = true)
    {
        var repo = new Mock<IRepositorioSesiones>();
        var identidad = new Mock<IClienteIdentidadUsuarios>();
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.EstaAutenticado).Returns(autenticado);
        usuario.Setup(u => u.Id).Returns(usuarioId);
        usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(c => c.Any(roles.Contains));

        return (
            new ListarSesionesManejador(repo.Object, usuario.Object, identidad.Object),
            repo,
            identidad);
    }

    [Fact]
    public async Task NoAutenticado_DebeLanzarUsuarioNoAutorizado()
    {
        var (manejador, _, _) = Crear(Array.Empty<string>(), autenticado: false);
        Func<Task> a = () => manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);
        await a.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Fact]
    public async Task Participante_DebeLanzarUsuarioNoAutorizado()
    {
        var (manejador, _, _) = Crear(new[] { "Participante" });
        Func<Task> a = () => manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);
        await a.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Fact]
    public async Task Administrador_VeTodasLasSesiones_SinPreguntarAIdentidad()
    {
        var (manejador, repo, identidad) = Crear(new[] { "Administrador" }, Admin);
        repo.Setup(r => r.ListarAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>
            {
                Sesion(Admin),
                Sesion(OperadorA),
                Sesion(OperadorB)
            });

        var lista = await manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);

        lista.Should().HaveCount(3);
        identidad.Verify(i => i.FiltrarAdministradoresAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Operador_VeSusPropiasYCreadasPorAdministrador()
    {
        var (manejador, repo, identidad) = Crear(new[] { "Operador" }, OperadorA);

        var propia = Sesion(OperadorA);
        var deAdmin = Sesion(Admin);
        var deAdmin2 = Sesion(Admin2);
        var deOtroOperador = Sesion(OperadorB);

        repo.Setup(r => r.ListarAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { propia, deAdmin, deAdmin2, deOtroOperador });

        // Identidad reporta Admin y Admin2 como administradores; OperadorB no.
        identidad
            .Setup(i => i.FiltrarAdministradoresAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.Where(id => id == Admin || id == Admin2).ToList());

        var lista = await manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);

        lista.Select(x => x.Id).Should().BeEquivalentTo(new[]
        {
            propia.Id, deAdmin.Id, deAdmin2.Id
        });
        lista.Should().NotContain(x => x.Id == deOtroOperador.Id);
    }

    [Fact]
    public async Task Operador_SoloPreguntaAIdentidadPorCreadoresAjenos()
    {
        var (manejador, repo, identidad) = Crear(new[] { "Operador" }, OperadorA);

        var propia1 = Sesion(OperadorA);
        var propia2 = Sesion(OperadorA);
        var deAdmin = Sesion(Admin);
        var deOtroOperador = Sesion(OperadorB);

        repo.Setup(r => r.ListarAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { propia1, propia2, deAdmin, deOtroOperador });

        identidad
            .Setup(i => i.FiltrarAdministradoresAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.Where(id => id == Admin).ToList());

        await manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);

        identidad.Verify(i => i.FiltrarAdministradoresAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids =>
                ids.Contains(Admin)
                && ids.Contains(OperadorB)
                && !ids.Contains(OperadorA)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Operador_NoVeSesionesDeOtroOperador()
    {
        var (manejador, repo, identidad) = Crear(new[] { "Operador" }, OperadorA);

        var deOtroOperador = Sesion(OperadorB);
        repo.Setup(r => r.ListarAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { deOtroOperador });

        // Identidad reporta que OperadorB NO es administrador.
        identidad
            .Setup(i => i.FiltrarAdministradoresAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var lista = await manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task Filtros_DebenPropagarseAlRepositorio()
    {
        var (manejador, repo, _) = Crear(new[] { "Administrador" }, Admin);
        repo.Setup(r => r.ListarAsync(
                TipoJuego.Trivia, EstadoSesion.Programada, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>());

        await manejador.Handle(
            new ListarSesionesConsulta(TipoJuego.Trivia, EstadoSesion.Programada),
            CancellationToken.None);

        repo.Verify(r => r.ListarAsync(
            TipoJuego.Trivia, EstadoSesion.Programada, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DtoListado_NoExponeCreadaPorRol()
    {
        typeof(SesionesServicio.Commons.Dtos.SesionListadoDto)
            .GetProperty("CreadaPorRol").Should().BeNull();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Operador_SiIdentidadDevuelveVacio_SoloVeSusPropias()
    {
        // Defensa contra regresión: el cliente HTTP de identidad ya
        // devuelve lista vacía cuando hay problemas (URL no configurada,
        // identidad caído, 4xx/5xx). El manejador NO debe explotar; el
        // Operador debe ver al menos sus propias sesiones.
        var (manejador, repo, identidad) = Crear(new[] { "Operador" }, OperadorA);

        var propia = Sesion(OperadorA);
        var deAdmin = Sesion(Admin);
        var deOtroOperador = Sesion(OperadorB);

        repo.Setup(r => r.ListarAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { propia, deAdmin, deOtroOperador });

        identidad
            .Setup(i => i.FiltrarAdministradoresAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var lista = await manejador.Handle(
            new ListarSesionesConsulta(null, null), CancellationToken.None);

        lista.Should().ContainSingle();
        lista[0].Id.Should().Be(propia.Id);
    }
}
