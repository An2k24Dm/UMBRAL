using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU40 — Orquestación de CrearEquipoManejador: rol Participante, sesión
// grupal En Preparación, hasheo solo en privados y respuesta sin secretos.
public class CrearEquipoManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Participante = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid SesionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IHashContrasenaEquipo> Hash { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Mock<IConsultasSesiones> Consultas { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Sesion? Actualizada;

        public Contexto(Sesion? sesion = null)
        {
            // Por defecto el participante no está en ninguna sesión activa.
            Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SesionParticipacionActivaDto?)null);

            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(Participante);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, "Participante") >= 0);

            Hash.Setup(h => h.Hashear(It.IsAny<string>())).Returns("HASH::seguro");

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion ?? SesionGrupalEnPreparacion());
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((s, _) => Actualizada = s)
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquiposSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public CrearEquipoManejador Construir()
            => new(new ValidadorCrearEquipo(), Repo.Object, Unidad.Object,
                Usuario.Object, Hash.Object, Reloj.Object,
                new PoliticaParticipacionUnicaSesion(Consultas.Object),
                Notificador.Object,
                Mock.Of<IRegistroLogsAplicacion>(),
                Mock.Of<IPublicadorEventosRanking>());
    }

    private static SesionGrupal SesionGrupalEnPreparacion(
        int maximoEquipos = 5, int maximoParticipantesPorEquipo = 3)
        => SesionGrupal.Rehidratar(
            SesionId, "Sesión", "Demo", EstadoSesion.EnPreparacion,
            AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, null, null,
            maximoEquipos, maximoParticipantesPorEquipo);

    private static SesionGrupal SesionGrupalEnEstado(EstadoSesion estado)
        => SesionGrupal.Rehidratar(
            SesionId, "Sesión", "Demo", estado,
            AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, null, null, 5, 3);

    private static CrearEquipoComando Comando(
        string nombre = "Rojo", TipoEquipoDto tipo = TipoEquipoDto.Publico,
        string? contrasena = null)
        => new(SesionId, new CrearEquipoDto
        {
            Nombre = nombre,
            Tipo = tipo,
            Contrasena = contrasena
        });

    [Fact]
    public async Task Participante_CreaEquipoPublico_EnSesionGrupalEnPreparacion()
    {
        var ctx = new Contexto();

        var dto = await ctx.Construir().Handle(Comando(), CancellationToken.None);

        dto.Nombre.Should().Be("Rojo");
        dto.Tipo.Should().Be("Publico");
        dto.LiderParticipanteId.Should().NotBeEmpty();
        dto.CapacidadMaxima.Should().Be(3);
        dto.CantidadParticipantes.Should().Be(1);
        dto.Puntaje.Should().Be(0);
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            SesionId, dto.Id, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            SesionId, dto.Id, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            SesionId, EstadoSesion.EnPreparacion.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Participante_CreaEquipoPrivado_HasheaLaContrasena()
    {
        var ctx = new Contexto();

        var dto = await ctx.Construir().Handle(
            Comando("Azul", TipoEquipoDto.Privado, "secreta"), CancellationToken.None);

        dto.Tipo.Should().Be("Privado");
        ctx.Hash.Verify(h => h.Hashear("secreta"), Times.Once);

        var equipo = ((SesionGrupal)ctx.Actualizada!).Equipos[0];
        equipo.ContrasenaHash!.Valor.Should().Be("HASH::seguro");
    }

    [Fact]
    public async Task EquipoPublico_NoLlamaAlHasher()
    {
        var ctx = new Contexto();
        await ctx.Construir().Handle(Comando(), CancellationToken.None);
        ctx.Hash.Verify(h => h.Hashear(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SesionIndividual_NoPermiteCrearEquipo()
    {
        var individual = SesionIndividual.Rehidratar(
            SesionId, "Ind", "Demo", EstadoSesion.EnPreparacion,
            AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, null, null, 10);
        var ctx = new Contexto(individual);

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task SesionNoEnPreparacion_NoPermiteCrearEquipo(EstadoSesion estado)
    {
        var ctx = new Contexto(SesionGrupalEnEstado(estado));

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_LanzaSesionNoEncontrada()
    {
        var ctx = new Contexto();
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task Administrador_NoPuedeCrearEquipo()
    {
        var ctx = new Contexto();
        ctx.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, "Administrador") >= 0);

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task Operador_NoPuedeCrearEquipo()
    {
        var ctx = new Contexto();
        ctx.Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, "Operador") >= 0);

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task NombreDuplicado_Lanza()
    {
        var sesion = SesionGrupalEnPreparacion();
        sesion.CrearEquipo(
            SesionesServicio.Dominio.ObjetosValor.NombreEquipo.Crear("Rojo"),
            TipoEquipo.Publico, null, Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Construir().Handle(Comando("rojo"), CancellationToken.None);
        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public async Task ParticipanteYaEnOtroEquipo_Lanza()
    {
        var sesion = SesionGrupalEnPreparacion();
        // El mismo participante autenticado ya lidera un equipo de la sesión.
        sesion.CrearEquipo(
            SesionesServicio.Dominio.ObjetosValor.NombreEquipo.Crear("Rojo"),
            TipoEquipo.Publico, null, Participante, AhoraUtc, AhoraUtc);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Construir().Handle(Comando("Azul"), CancellationToken.None);
        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public async Task PrivadoSinContrasena_LanzaValidacion()
    {
        var ctx = new Contexto();
        Func<Task> accion = () => ctx.Construir().Handle(
            Comando("Azul", TipoEquipoDto.Privado, null), CancellationToken.None);
        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task Respuesta_NoExponeContrasenaNiHash()
    {
        var ctx = new Contexto();
        var dto = await ctx.Construir().Handle(
            Comando("Azul", TipoEquipoDto.Privado, "secreta"), CancellationToken.None);

        // CrearEquipoRespuestaDto no tiene campos de contraseña/hash por diseño;
        // se verifica que ningún valor textual filtre el secreto.
        dto.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => (string?)p.GetValue(dto))
            .Should().NotContain(v => v != null && v.Contains("secreta"));
    }

    // ---- Regla de participación única ----

    private static void ConParticipacionEn(
        Contexto ctx, Guid sesionId, EstadoSesion estado)
        => ctx.Consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SesionParticipacionActivaDto(
                sesionId, "Otra", estado, ModoSesion.Grupal, null, null));

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task NoCrea_SiParticipanteEnOtraSesionActiva(EstadoSesion estado)
    {
        var ctx = new Contexto();
        ConParticipacionEn(ctx, Guid.NewGuid(), estado);

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<ParticipanteYaEstaEnSesionActivaExcepcion>();
    }

    [Fact]
    public async Task Crea_SiParticipacionAnteriorNoBloqueante()
    {
        // Finalizada/Cancelada hacen que la consulta no devuelva participación.
        var ctx = new Contexto();
        var dto = await ctx.Construir().Handle(Comando(), CancellationToken.None);
        dto.Nombre.Should().Be("Rojo");
    }

    [Fact]
    public async Task NoCrea_SiYaPerteneceAEstaMismaSesion()
    {
        var ctx = new Contexto();
        ConParticipacionEn(ctx, SesionId, EstadoSesion.EnPreparacion);

        Func<Task> accion = () => ctx.Construir().Handle(Comando(), CancellationToken.None);
        await accion.Should().ThrowAsync<ParticipanteYaPerteneceASesionExcepcion>();
    }
}
