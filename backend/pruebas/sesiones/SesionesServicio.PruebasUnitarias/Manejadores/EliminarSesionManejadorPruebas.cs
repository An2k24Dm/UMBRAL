using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.CasosDeUso.Manejadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// Orquestación de EliminarSesionManejador (HU39): autenticación, rol Operador,
// propiedad de la sesión y estado Programada.
public class EliminarSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtroOperador = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SesionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();

        public Contexto(Sesion? sesion, string rol = "Operador", Guid? usuarioId = null)
        {
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repo.Setup(r => r.EliminarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public EliminarSesionManejador Construir()
            => new(Repo.Object, Unidad.Object, Usuario.Object);
    }

    private static SesionIndividual SesionDe(
        Guid operador, EstadoSesion estado = EstadoSesion.Programada)
        => SesionIndividual.Rehidratar(
            SesionId, "Original", "Demo", estado,
            AhoraUtc.AddHours(2), "CODE-ORIG", operador, AhoraUtc,
            null, null, 10);

    [Fact]
    public async Task Operador_EliminaSesionPropiaProgramada()
    {
        var ctx = new Contexto(SesionDe(Operador));

        await ctx.Construir().Handle(new EliminarSesionComando(SesionId), CancellationToken.None);

        ctx.Repo.Verify(r => r.EliminarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Once);
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Operador_NoPuedeEliminarSesionDeOtroOperador()
    {
        var ctx = new Contexto(SesionDe(OtroOperador), rol: "Operador", usuarioId: Operador);

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarSesionComando(SesionId), CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        ctx.Repo.Verify(r => r.EliminarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Participante")]
    public async Task RolNoOperador_NoPuedeEliminar(string rol)
    {
        var ctx = new Contexto(SesionDe(Operador), rol: rol);

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarSesionComando(SesionId), CancellationToken.None);

        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
        ctx.Repo.Verify(r => r.EliminarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SesionInexistente_LanzaNoEncontrada()
    {
        var ctx = new Contexto(sesion: null);

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarSesionComando(SesionId), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task NoSePuedeEliminarSiNoEstaProgramada(EstadoSesion estado)
    {
        var ctx = new Contexto(SesionDe(Operador, estado));

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarSesionComando(SesionId), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEliminableExcepcion>();
        ctx.Repo.Verify(r => r.EliminarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
