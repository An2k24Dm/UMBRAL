using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.ModificarSesion;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// Orquestación de ModificarSesionManejador (HU38): autenticación, rol,
// propiedad, estado Programada, validación de misiones y aplicación de cambios.
public class ModificarSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtroOperador = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SesionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MisionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MisionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string CodigoOriginal = "CODE-ORIG";

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IClienteJuegosMisiones> Misiones { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Sesion? Persistida;

        private static readonly IFabricaSesion FabricaSesionReal =
            new FabricaSesion(new ICreadorSesion[]
            {
                new CreadorSesionIndividual(),
                new CreadorSesionGrupal()
            });

        private static readonly FabricaMapeadorDetalleSesion FabricaMapeador =
            new(new IMapeadorDetalleSesion[]
            {
                new MapeadorDetalleSesionIndividual(),
                new MapeadorDetalleSesionGrupal()
            });

        public Contexto(Sesion? sesionExistente = null, string rol = "Operador", Guid? usuarioId = null)
        {
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesionExistente);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((s, _) => Persistida = s)
                .Returns(Task.CompletedTask);

            foreach (var m in new[] { MisionA, MisionB })
                Misiones.Setup(c => c.ObtenerMisionAsync(m, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new MisionResumenJuegosDto
                    {
                        Id = m,
                        Nombre = m.ToString(),
                        Estado = "Activa",
                        TotalEtapas = 2,
                        TiempoTotalSegundos = m == MisionA ? 90 : 95
                    });
        }

        public ModificarSesionManejador Construir()
            => new(new ValidadorModificarSesion(), Repo.Object, Unidad.Object,
                Usuario.Object, new ValidadorMisionesSesion(Misiones.Object),
                Reloj.Object, FabricaSesionReal, FabricaMapeador,
                Mock.Of<IRegistroLogsAplicacion>());
    }

    private static IEnumerable<SesionMision> Misiones(Guid sesionId, params Guid[] ids)
        => ids.Select((m, i) => SesionMision.Rehidratar(Guid.NewGuid(), sesionId, m, i + 1));

    private static SesionIndividual IndividualProgramada(
        Guid operador, EstadoSesion estado = EstadoSesion.Programada,
        int maxParticipantes = 10, IEnumerable<Participante>? participantes = null)
        => SesionIndividual.Rehidratar(
            SesionId, "Original", "Descripción original", estado,
            AhoraUtc.AddHours(2), CodigoOriginal, operador, AhoraUtc,
            null, null, maxParticipantes,
            Misiones(SesionId, MisionA), participantes);

    private static SesionGrupal GrupalProgramada(
        Guid operador, int maxEquipos = 5, int maxPorEquipo = 2,
        IEnumerable<Equipo>? equipos = null)
        => SesionGrupal.Rehidratar(
            SesionId, "Original", "Descripción original", EstadoSesion.Programada,
            AhoraUtc.AddHours(2), CodigoOriginal, operador, AhoraUtc,
            null, null, maxEquipos, maxPorEquipo,
            Misiones(SesionId, MisionA), equipos);

    private static ModificarSesionDto DtoIndividual(int maxParticipantes = 20, List<Guid>? misiones = null)
        => new()
        {
            Nombre = "Editada",
            Descripcion = "Descripción editada",
            Modo = "Individual",
            FechaProgramada = AhoraUtc.AddHours(5),
            MisionesIds = misiones ?? new List<Guid> { MisionA },
            MaximoParticipantes = maxParticipantes
        };

    private static ModificarSesionDto DtoGrupal(int maxEquipos = 4, int maxPorEquipo = 3)
        => new()
        {
            Nombre = "Editada",
            Descripcion = "Descripción editada",
            Modo = "Grupal",
            FechaProgramada = AhoraUtc.AddHours(5),
            MisionesIds = new List<Guid> { MisionA },
            MaximoEquipos = maxEquipos,
            MaximoParticipantesPorEquipo = maxPorEquipo
        };

    [Fact]
    public async Task Operador_ModificaSuPropiaSesion_ActualizaDatos()
    {
        var ctx = new Contexto(IndividualProgramada(Operador));

        var detalle = await ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoIndividual(maxParticipantes: 25)),
            CancellationToken.None);

        detalle.Nombre.Should().Be("Editada");
        detalle.Descripcion.Should().Be("Descripción editada");
        detalle.MaximoParticipantes.Should().Be(25);
        ctx.Persistida.Should().BeOfType<SesionIndividual>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Operador_NoPuedeModificarSesionDeOtroOperador()
    {
        var ctx = new Contexto(IndividualProgramada(OtroOperador), rol: "Operador", usuarioId: Operador);

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoIndividual()), CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task Administrador_NoPuedeModificarSesion()
    {
        // El Administrador tiene acceso de solo lectura: no puede modificar.
        var ctx = new Contexto(IndividualProgramada(Operador), rol: "Administrador");

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoIndividual()), CancellationToken.None);

        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task NoSePuedeModificarSiNoEstaProgramada(EstadoSesion estado)
    {
        var ctx = new Contexto(IndividualProgramada(Operador, estado));

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoIndividual()), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoModificableExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_LanzaNoEncontrada()
    {
        var ctx = new Contexto(sesionExistente: null);

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoIndividual()), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task NoModificaCodigoAccesoNiEstado()
    {
        var ctx = new Contexto(IndividualProgramada(Operador));

        await ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoIndividual()), CancellationToken.None);

        ctx.Persistida!.CodigoAcceso.Should().Be(CodigoOriginal);
        ctx.Persistida!.Estado.Should().Be(EstadoSesion.Programada);
        ctx.Persistida!.Id.Should().Be(SesionId);
    }

    [Fact]
    public async Task ActualizaFechaYMisiones()
    {
        var ctx = new Contexto(IndividualProgramada(Operador));
        var dto = DtoIndividual(misiones: new List<Guid> { MisionB });

        var detalle = await ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, dto), CancellationToken.None);

        detalle.FechaProgramada.Should().Be(AhoraUtc.AddHours(5));
        detalle.Misiones.Select(m => m.MisionId).Should().Equal(new[] { MisionB });
    }

    [Fact]
    public async Task ActualizaDuracionDesdeMisionesSeleccionadas()
    {
        var ctx = new Contexto(IndividualProgramada(Operador));
        var dto = DtoIndividual(misiones: new List<Guid> { MisionA, MisionB });

        await ctx.Construir().Handle(new ModificarSesionComando(SesionId, dto), CancellationToken.None);

        ctx.Persistida!.DuracionSegundosLimite.Should().Be(185);
        ctx.Misiones.Verify(c => c.ObtenerMisionAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MisionesDuplicadas_LanzaValidacion()
    {
        var ctx = new Contexto(IndividualProgramada(Operador));
        var dto = DtoIndividual(misiones: new List<Guid> { MisionA, MisionA });

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, dto), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task ModoInvalido_LanzaValidacion()
    {
        var ctx = new Contexto(IndividualProgramada(Operador));
        var dto = DtoIndividual();
        dto.Modo = "Hibrida";

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, dto), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task Grupal_ActualizaCapacidades()
    {
        var ctx = new Contexto(GrupalProgramada(Operador));

        var detalle = await ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoGrupal(maxEquipos: 8, maxPorEquipo: 4)),
            CancellationToken.None);

        detalle.Modo.Should().Be("Grupal");
        detalle.MaximoEquipos.Should().Be(8);
        detalle.MaximoParticipantesPorEquipo.Should().Be(4);
    }

    [Fact]
    public async Task CambiarTipo_SinInscritos_ConservaIdentidad()
    {
        // Individual sin participantes -> Grupal.
        var ctx = new Contexto(IndividualProgramada(Operador));

        var detalle = await ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoGrupal()), CancellationToken.None);

        detalle.Modo.Should().Be("Grupal");
        ctx.Persistida.Should().BeOfType<SesionGrupal>();
        ctx.Persistida!.Id.Should().Be(SesionId);
        ctx.Persistida!.CodigoAcceso.Should().Be(CodigoOriginal);
        ctx.Persistida!.Estado.Should().Be(EstadoSesion.Programada);
    }

    [Fact]
    public async Task CambiarTipo_ConParticipantes_SeRechaza()
    {
        var participante = Participante.Rehidratar(
            Guid.NewGuid(), SesionId, Guid.NewGuid(), null, 0, AhoraUtc, null);
        var ctx = new Contexto(
            IndividualProgramada(Operador, participantes: new[] { participante }));

        Func<Task> accion = () => ctx.Construir().Handle(
            new ModificarSesionComando(SesionId, DtoGrupal()), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
    }
}
