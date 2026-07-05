using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.ModificarEquipo;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU41 — Orquestación de ModificarEquipoManejador: rol Participante, líder,
// hasheo solo en privados y respuesta sin secretos.
public class ModificarEquipoManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IHashContrasenaEquipo> Hash { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Sesion? Actualizada;
        public Guid SesionId;
        public Guid EquipoId;

        public Contexto(Sesion? sesion = null, Guid? equipoId = null, Guid? usuarioId = null)
        {
            var s = sesion ?? SesionConEquipo(Lider, out var eq, TipoEquipo.Publico, null);
            if (sesion is not null && equipoId is null && s is SesionGrupal g && g.Equipos.Any())
                equipoId = g.Equipos.First().Id;

            SesionId = s.Id;
            EquipoId = equipoId ?? (s is SesionGrupal gg && gg.Equipos.Any()
                ? gg.Equipos.First().Id : Guid.NewGuid());

            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Lider);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, "Participante") >= 0);

            Hash.Setup(h => h.Hashear(It.IsAny<string>())).Returns("HASH::seguro");

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(s);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((x, _) => Actualizada = x)
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquiposSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public ModificarEquipoManejador Construir()
            => new(new ValidadorModificarEquipo(), Repo.Object, Unidad.Object,
                Usuario.Object, Hash.Object, Notificador.Object,
                Mock.Of<IRegistroLogsAplicacion>());
    }

    private static SesionGrupal SesionConEquipo(
        Guid lider, out Guid equipoId,
        TipoEquipo tipo = TipoEquipo.Publico,
        ContrasenaEquipoHash? hash = null)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), tipo, hash, lider, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        sesion.Preparar();
        return sesion;
    }

    private static ModificarEquipoComando Comando(
        Guid sesionId, Guid equipoId, string nombre = "Azul",
        TipoEquipoDto tipo = TipoEquipoDto.Publico, string? contrasena = null)
        => new(sesionId, equipoId, new ModificarEquipoDto
        {
            Nombre = nombre,
            Tipo = tipo,
            Contrasena = contrasena
        });

    [Fact]
    public async Task Lider_ModificaNombre()
    {
        var ctx = new Contexto();
        var dto = await ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId, "Azul"), CancellationToken.None);

        dto.Nombre.Should().Be("Azul");
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, ctx.EquipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            ctx.SesionId, ctx.EquipoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoLider_Responde403()
    {
        var ctx = new Contexto(usuarioId: Guid.NewGuid());
        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task Operador_NoPuedeModificar()
    {
        var ctx = new Contexto();
        ctx.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, "Operador") >= 0);

        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task Administrador_NoPuedeModificar()
    {
        var ctx = new Contexto();
        ctx.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, "Administrador") >= 0);

        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task SesionIndividual_Rechaza()
    {
        var individual = SesionIndividual.Crear(
            "Ind", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        var ctx = new Contexto(individual, equipoId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<SesionNoGrupalExcepcion>();
    }

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var ctx = new Contexto();
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task PublicoAPrivado_LlamaHasher()
    {
        var ctx = new Contexto();
        await ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId, "Rojo", TipoEquipoDto.Privado, "secreta"),
            CancellationToken.None);

        ctx.Hash.Verify(h => h.Hashear("secreta"), Times.Once);
        var equipo = ((SesionGrupal)ctx.Actualizada!).Equipos.First();
        equipo.Tipo.Should().Be(TipoEquipo.Privado);
        equipo.ContrasenaHash!.Valor.Should().Be("HASH::seguro");
    }

    [Fact]
    public async Task PrivadoAPublico_LimpiaHash_SinHasher()
    {
        var sesion = SesionConEquipo(
            Lider, out var equipoId, TipoEquipo.Privado, ContrasenaEquipoHash.Crear("previo"));
        var ctx = new Contexto(sesion, equipoId);

        await ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId, "Rojo", TipoEquipoDto.Publico),
            CancellationToken.None);

        ctx.Hash.Verify(h => h.Hashear(It.IsAny<string>()), Times.Never);
        var equipo = ((SesionGrupal)ctx.Actualizada!).Equipos.First();
        equipo.Tipo.Should().Be(TipoEquipo.Publico);
        equipo.ContrasenaHash.Should().BeNull();
    }

    [Fact]
    public async Task PublicoAPrivadoSinContrasena_LanzaValidacion()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            Comando(ctx.SesionId, ctx.EquipoId, "Rojo", TipoEquipoDto.Privado, null),
            CancellationToken.None);
        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public void RespuestaDto_NoExponeContrasenaNiHash()
    {
        var props = typeof(ModificarEquipoRespuestaDto)
            .GetProperties().Select(p => p.Name.ToLowerInvariant());
        props.Should().NotContain(n => n.Contains("contrasena") || n.Contains("hash"));
    }
}
