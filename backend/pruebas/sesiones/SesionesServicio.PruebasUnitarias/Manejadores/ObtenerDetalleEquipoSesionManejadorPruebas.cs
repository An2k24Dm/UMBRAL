using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerDetalleEquipoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU43 — ObtenerDetalleEquipoSesionManejador: integrantes, líder, identidad y fallback.
public class ObtenerDetalleEquipoSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Miembro = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static SesionGrupal SesionConEquipo(out Guid sesionId, out Guid equipoId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        sesion.AgregarParticipanteAEquipo(equipo.Id, Miembro, AhoraUtc, AhoraUtc);
        sesionId = sesion.Id;
        equipoId = equipo.Id;
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

    private static Mock<IClienteIdentidadParticipantes> IdentidadCon(
        params (Guid Id, string Nombre, string Apellido, string Alias)[] datos)
    {
        var mock = new Mock<IClienteIdentidadParticipantes>();
        var dic = datos.ToDictionary(
            d => d.Id,
            d => new ParticipanteIdentidadResumenDto
            {
                Id = d.Id, Nombre = d.Nombre, Apellido = d.Apellido, Alias = d.Alias
            });
        mock.Setup(c => c.ObtenerParticipantesPorIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>)dic);
        return mock;
    }

    private static ObtenerDetalleEquipoSesionManejador Construir(
        Sesion sesion, Guid sesionId,
        Mock<IUsuarioActual> usuario,
        Mock<IClienteIdentidadParticipantes> identidad)
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        return new ObtenerDetalleEquipoSesionManejador(
            repo.Object, usuario.Object, identidad.Object);
    }

    [Fact]
    public async Task Participante_VeDetalle_ConIntegrantesYLider()
    {
        var sesion = SesionConEquipo(out var sesionId, out var equipoId);
        var identidad = IdentidadCon(
            (Lider, "Ana", "García", "ana"),
            (Miembro, "Beto", "Pérez", "beto"));
        var manejador = Construir(sesion, sesionId, Usuario(Lider, "Participante"), identidad);

        var dto = await manejador.Handle(
            new ObtenerDetalleEquipoSesionConsulta(sesionId, equipoId), CancellationToken.None);

        dto.Nombre.Should().Be("Rojo");
        dto.CapacidadMaxima.Should().Be(3);
        dto.CantidadParticipantes.Should().Be(2);
        dto.Participantes.Should().HaveCount(2);
        // El líder va primero.
        dto.Participantes[0].EsLider.Should().BeTrue();
        dto.Participantes[0].Nombre.Should().Be("Ana");
        dto.Participantes[0].Alias.Should().Be("ana");
        dto.Participantes[0].Apellido.Should().Be("García");
        dto.Participantes[0].FechaUnion.Should().Be(AhoraUtc);
        dto.FechaCreacion.Should().Be(AhoraUtc);
        dto.EsMiEquipo.Should().BeTrue();
        dto.SoyLider.Should().BeTrue();
    }

    [Fact]
    public async Task Operador_VeDetalle_DeSuSesion()
    {
        var sesion = SesionConEquipo(out var sesionId, out var equipoId);
        var manejador = Construir(
            sesion, sesionId, Usuario(Operador, "Operador"),
            IdentidadCon((Lider, "Ana", "García", "ana")));

        var dto = await manejador.Handle(
            new ObtenerDetalleEquipoSesionConsulta(sesionId, equipoId), CancellationToken.None);

        dto.LiderParticipanteId.Should().NotBeEmpty();
        dto.EsMiEquipo.Should().BeFalse();
    }

    [Fact]
    public async Task EquipoDeOtraSesion_LanzaEquipoNoEncontrado()
    {
        var sesion = SesionConEquipo(out var sesionId, out _);
        var manejador = Construir(
            sesion, sesionId, Usuario(Lider, "Participante"),
            IdentidadCon());

        Func<Task> accion = () => manejador.Handle(
            new ObtenerDetalleEquipoSesionConsulta(sesionId, Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task DevuelvePuntajeIndividual()
    {
        var sesion = SesionConEquipo(out var sesionId, out var equipoId);
        var manejador = Construir(
            sesion, sesionId, Usuario(Lider, "Participante"),
            IdentidadCon((Lider, "Ana", "García", "ana"), (Miembro, "Beto", "Pérez", "beto")));

        var dto = await manejador.Handle(
            new ObtenerDetalleEquipoSesionConsulta(sesionId, equipoId), CancellationToken.None);

        dto.Participantes.Should().OnlyContain(p => p.Puntaje == 0);
    }

    [Fact]
    public async Task SinDatosEnIdentidad_UsaFallback()
    {
        var sesion = SesionConEquipo(out var sesionId, out var equipoId);
        // Identidad no resuelve a nadie.
        var manejador = Construir(
            sesion, sesionId, Usuario(Lider, "Participante"), IdentidadCon());

        var dto = await manejador.Handle(
            new ObtenerDetalleEquipoSesionConsulta(sesionId, equipoId), CancellationToken.None);

        dto.Participantes.Should().OnlyContain(p => p.Nombre == "Participante");
        dto.Participantes.Should().OnlyContain(p => p.Alias == "Participante");
    }

    [Fact]
    public void DtoIntegrante_NoExponeDatosSensibles()
    {
        var propiedades = typeof(SesionesServicio.Commons.Dtos.IntegranteEquipoDto)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant());

        propiedades.Should().NotContain(nombre =>
            nombre.Contains("correo") || nombre.Contains("telefono") ||
            nombre.Contains("contrasena") || nombre.Contains("hash") ||
            nombre.Contains("keycloak") || nombre.Contains("direccion") ||
            nombre.Contains("nacimiento"));
    }
}
