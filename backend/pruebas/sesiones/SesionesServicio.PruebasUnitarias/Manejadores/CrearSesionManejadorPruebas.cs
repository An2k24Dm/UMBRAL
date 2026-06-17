using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.CasosDeUso.Manejadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// Cubre la orquestación de CrearSesionManejador: rol Operador, fecha
// futura, validación de misiones contra juegos-servicio, la fábrica
// (IFabricaSesion) instanciando la subclase correcta y la sesión naciendo vacía.
public class CrearSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid MisionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MisionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid MisionInactiva = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid MisionInexistente = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid MisionSinEtapas = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IClienteJuegosMisiones> Misiones { get; } = new();
        public Mock<IGeneradorCodigoAcceso> Generador { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Sesion? Persistida;

        public Contexto()
        {
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Generador.Setup(g => g.Generar()).Returns("CODE99");
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, "Operador") >= 0);

            Repo.Setup(r => r.AgregarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((s, _) => Persistida = s)
                .Returns(Task.CompletedTask);

            Misiones.Setup(c => c.ObtenerMisionAsync(MisionA, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionResumenJuegosDto
                { Id = MisionA, Nombre = "A", Estado = "Activa", TotalEtapas = 2 });
            Misiones.Setup(c => c.ObtenerMisionAsync(MisionB, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionResumenJuegosDto
                { Id = MisionB, Nombre = "B", Estado = "Activa", TotalEtapas = 1 });
            Misiones.Setup(c => c.ObtenerMisionAsync(MisionInactiva, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionResumenJuegosDto
                { Id = MisionInactiva, Nombre = "Inactiva", Estado = "Inactiva", TotalEtapas = 2 });
            Misiones.Setup(c => c.ObtenerMisionAsync(MisionInexistente, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MisionResumenJuegosDto?)null);
            Misiones.Setup(c => c.ObtenerMisionAsync(MisionSinEtapas, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionResumenJuegosDto
                { Id = MisionSinEtapas, Nombre = "Vacía", Estado = "Activa", TotalEtapas = 0 });
        }

        // Se usa la fábrica real con sus creadores de dominio: son piezas
        // puras sin dependencias externas, así la prueba ejercita el camino
        // real de selección por modo.
        private static readonly IFabricaSesion FabricaSesionReal =
            new FabricaSesion(new ICreadorSesion[]
            {
                new CreadorSesionIndividual(),
                new CreadorSesionGrupal()
            });

        public CrearSesionManejador Construir()
            => new(new ValidadorCrearSesion(), Repo.Object, Unidad.Object,
                Usuario.Object, new ValidadorMisionesSesion(Misiones.Object),
                Generador.Object, Reloj.Object, FabricaSesionReal);
    }

    private static CrearSesionSolicitudDto DtoValido(
        string modo = "Individual", List<Guid>? misiones = null) => new()
        {
            Nombre = "Sesión piloto",
            Descripcion = "Demo",
            Modo = modo,
            FechaProgramada = AhoraUtc.AddHours(1),
            MisionesIds = misiones ?? new List<Guid> { MisionA },
            // Capacidad para ambos modos; el creador toma la que aplica.
            MaximoParticipantes = 10,
            MaximoEquipos = 5,
            MaximoParticipantesPorEquipo = 2
        };

    [Fact]
    public async Task Operador_CreaSesionIndividual_GuardaSesionIndividual()
    {
        var ctx = new Contexto();
        var manejador = ctx.Construir();

        var respuesta = await manejador.Handle(
            new CrearSesionComando(DtoValido("Individual")), CancellationToken.None);

        respuesta.Modo.Should().Be("Individual");
        respuesta.Estado.Should().Be("Programada");
        respuesta.CodigoAcceso.Should().Be("CODE99");
        respuesta.OperadorCreadorId.Should().Be(Operador);
        respuesta.MisionesIds.Should().Equal(new[] { MisionA });
        ctx.Persistida.Should().BeOfType<SesionIndividual>();
        ((SesionIndividual)ctx.Persistida!).Participantes.Should().BeEmpty();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Operador_CreaSesionGrupal_GuardaSesionGrupal()
    {
        var ctx = new Contexto();
        var manejador = ctx.Construir();

        var respuesta = await manejador.Handle(
            new CrearSesionComando(DtoValido("Grupal")), CancellationToken.None);

        respuesta.Modo.Should().Be("Grupal");
        ctx.Persistida.Should().BeOfType<SesionGrupal>();
        ((SesionGrupal)ctx.Persistida!).Equipos.Should().BeEmpty();
    }

    [Fact]
    public async Task Operador_CreaSesionIndividual_GuardaCapacidadConfigurada()
    {
        var ctx = new Contexto();
        var dto = DtoValido("Individual");
        dto.MaximoParticipantes = 25;

        await ctx.Construir().Handle(new CrearSesionComando(dto), CancellationToken.None);

        ((SesionIndividual)ctx.Persistida!).MaximoParticipantes.Should().Be(25);
    }

    [Fact]
    public async Task Operador_CreaSesionGrupal_GuardaCapacidadesConfiguradas()
    {
        var ctx = new Contexto();
        var dto = DtoValido("Grupal");
        dto.MaximoEquipos = 8;
        dto.MaximoParticipantesPorEquipo = 4;

        await ctx.Construir().Handle(new CrearSesionComando(dto), CancellationToken.None);

        var grupal = (SesionGrupal)ctx.Persistida!;
        grupal.MaximoEquipos.Should().Be(8);
        grupal.MaximoParticipantesPorEquipo.Should().Be(4);
    }

    [Fact]
    public async Task SesionIndividual_SinCapacidad_LanzaValidacion()
    {
        var ctx = new Contexto();
        var dto = DtoValido("Individual");
        dto.MaximoParticipantes = null;

        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(dto), CancellationToken.None);
        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task SesionGrupal_SinCapacidadDeEquipos_LanzaValidacion()
    {
        var ctx = new Contexto();
        var dto = DtoValido("Grupal");
        dto.MaximoEquipos = null;

        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(dto), CancellationToken.None);
        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task Operador_ConCincoMisionesActivas_CreaSesion()
    {
        var ctx = new Contexto();
        ctx.Misiones.Setup(c => c.ObtenerMisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new MisionResumenJuegosDto
            { Id = id, Nombre = id.ToString(), Estado = "Activa", TotalEtapas = 1 });
        var manejador = ctx.Construir();
        var cinco = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

        var respuesta = await manejador.Handle(
            new CrearSesionComando(DtoValido("Individual", cinco)), CancellationToken.None);

        respuesta.MisionesIds.Should().Equal(cinco);
    }

    [Fact]
    public async Task SinMisiones_LanzaValidacion()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido("Individual", new List<Guid>())),
            CancellationToken.None);
        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task MasDeCincoMisiones_LanzaValidacion()
    {
        var ctx = new Contexto();
        var seis = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido("Individual", seis)), CancellationToken.None);
        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task MisionInexistente_LanzaMisionNoEncontrada()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido("Individual", new List<Guid> { MisionInexistente })),
            CancellationToken.None);
        await accion.Should().ThrowAsync<MisionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task MisionInactiva_LanzaMisionNoActiva()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido("Individual", new List<Guid> { MisionInactiva })),
            CancellationToken.None);
        await accion.Should().ThrowAsync<MisionNoActivaExcepcion>();
    }

    [Fact]
    public async Task MisionSinEtapas_LanzaMisionSinEtapas()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido("Individual", new List<Guid> { MisionSinEtapas })),
            CancellationToken.None);
        await accion.Should().ThrowAsync<MisionSinEtapasExcepcion>();
    }

    [Fact]
    public async Task FechaPasada_LanzaSesionInvalida()
    {
        var ctx = new Contexto();
        var dto = DtoValido();
        dto.FechaProgramada = AhoraUtc.AddMinutes(-1);
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(dto), CancellationToken.None);
        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task Administrador_NoPuedeCrear()
    {
        var ctx = new Contexto();
        ctx.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, "Administrador") >= 0);
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido()), CancellationToken.None);
        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Fact]
    public async Task Participante_NoPuedeCrear()
    {
        var ctx = new Contexto();
        ctx.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, "Participante") >= 0);
        Func<Task> accion = () => ctx.Construir().Handle(
            new CrearSesionComando(DtoValido()), CancellationToken.None);
        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Fact]
    public async Task NoCreaParticipantesNiEquiposDuranteCrearSesion()
    {
        var ctx = new Contexto();
        await ctx.Construir().Handle(
            new CrearSesionComando(DtoValido("Grupal")), CancellationToken.None);

        var grupal = (SesionGrupal)ctx.Persistida!;
        grupal.Equipos.Should().BeEmpty();
    }
}
