using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ListarEquiposSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU43 — ListarEquiposSesionManejador: autorización por rol y mapeo del listado.
public class ListarEquiposSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtroOperador = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static SesionGrupal SesionConEquipos(out Guid sesionId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 3);
        sesion.CrearEquipo(NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        sesion.CrearEquipo(
            NombreEquipo.Crear("Azul"), TipoEquipo.Privado,
            ContrasenaEquipoHash.Crear("hash"), Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesionId = sesion.Id;
        return sesion;
    }

    private static Mock<IUsuarioActual> Usuario(Guid? id, string rol)
    {
        var mock = new Mock<IUsuarioActual>();
        mock.Setup(u => u.EstaAutenticado()).Returns(true);
        mock.Setup(u => u.ObtenerId()).Returns(id);
        mock.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);
        return mock;
    }

    private static ListarEquiposSesionManejador Construir(
        Sesion sesion, Guid sesionId, Mock<IUsuarioActual> usuario)
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        return new ListarEquiposSesionManejador(repo.Object, usuario.Object);
    }

    [Fact]
    public async Task Participante_ListaEquipos_DeSesionGrupal()
    {
        var sesion = SesionConEquipos(out var sesionId);
        var manejador = Construir(sesion, sesionId, Usuario(Lider, "Participante"));

        var resultado = await manejador.Handle(
            new ListarEquiposSesionConsulta(sesionId), CancellationToken.None);

        resultado.Should().HaveCount(2);
    }

    [Fact]
    public async Task Operador_ListaEquipos_DeSuSesion()
    {
        var sesion = SesionConEquipos(out var sesionId);
        var manejador = Construir(sesion, sesionId, Usuario(Operador, "Operador"));

        var resultado = await manejador.Handle(
            new ListarEquiposSesionConsulta(sesionId), CancellationToken.None);

        resultado.Should().HaveCount(2);
    }

    [Fact]
    public async Task Operador_NoListaEquipos_DeSesionAjena()
    {
        var sesion = SesionConEquipos(out var sesionId);
        var manejador = Construir(sesion, sesionId, Usuario(OtroOperador, "Operador"));

        Func<Task> accion = () => manejador.Handle(
            new ListarEquiposSesionConsulta(sesionId), CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task NoLista_SiSesionEsIndividual()
    {
        var individual = SesionIndividual.Crear(
            "Ind", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        var manejador = Construir(individual, individual.Id, Usuario(Lider, "Participante"));

        Func<Task> accion = () => manejador.Handle(
            new ListarEquiposSesionConsulta(individual.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoGrupalExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_LanzaNoEncontrada()
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);
        var manejador = new ListarEquiposSesionManejador(
            repo.Object, Usuario(Lider, "Participante").Object);

        Func<Task> accion = () => manejador.Handle(
            new ListarEquiposSesionConsulta(Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task Mapea_Nombre_Tipo_Capacidad_Puntaje()
    {
        var sesion = SesionConEquipos(out var sesionId);
        var manejador = Construir(sesion, sesionId, Usuario(Operador, "Operador"));

        var resultado = await manejador.Handle(
            new ListarEquiposSesionConsulta(sesionId), CancellationToken.None);

        var rojo = resultado.Single(e => e.Nombre == "Rojo");
        rojo.Tipo.Should().Be("Publico");
        rojo.CapacidadMaxima.Should().Be(3);
        rojo.CantidadParticipantes.Should().Be(1);
        rojo.Puntaje.Should().Be(0);
        rojo.EstaLleno.Should().BeFalse();
        rojo.FechaCreacion.Should().Be(AhoraUtc);

        var azul = resultado.Single(e => e.Nombre == "Azul");
        azul.Tipo.Should().Be("Privado");
    }

    [Fact]
    public async Task MarcaEsMiEquipoYSoyLider_ParaElLider()
    {
        var sesion = SesionConEquipos(out var sesionId);
        var manejador = Construir(sesion, sesionId, Usuario(Lider, "Participante"));

        var resultado = await manejador.Handle(
            new ListarEquiposSesionConsulta(sesionId), CancellationToken.None);

        var rojo = resultado.Single(e => e.Nombre == "Rojo");
        rojo.EsMiEquipo.Should().BeTrue();
        rojo.SoyLider.Should().BeTrue();

        var azul = resultado.Single(e => e.Nombre == "Azul");
        azul.EsMiEquipo.Should().BeFalse();
        azul.SoyLider.Should().BeFalse();
    }

    [Fact]
    public void Dto_NoExponeContrasenaNiHash()
    {
        var props = typeof(SesionesServicio.Commons.Dtos.EquipoSesionListadoDto)
            .GetProperties().Select(p => p.Name.ToLowerInvariant());
        props.Should().NotContain(n => n.Contains("contrasena") || n.Contains("hash"));
    }
}
