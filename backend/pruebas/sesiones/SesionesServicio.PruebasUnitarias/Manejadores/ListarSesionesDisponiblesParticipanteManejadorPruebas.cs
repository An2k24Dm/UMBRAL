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
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// Cubre las reglas del manejador móvil del listado:
//   * filtro "Todas" se normaliza a null antes de llegar al repositorio;
//   * la capacidad expuesta depende del subtipo concreto de Sesion;
//   * los DTOs no exponen OperadorCreadorId ni otros datos admin.
//
// La regla "el listado solo devuelve Programada/EnPreparacion/Activa"
// vive en infraestructura (repositorio) y está cubierta por las pruebas
// de integración del endpoint; aquí basta con verificar que el manejador
// delega correctamente.
public class ListarSesionesDisponiblesParticipanteManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionIndividual CrearIndividual(string nombre, EstadoSesion estado)
    {
        return SesionIndividual.Rehidratar(
            id: Guid.NewGuid(),
            nombre: nombre,
            descripcion: "Demo",
            estado: estado,
            fechaProgramada: AhoraUtc.AddHours(1),
            codigoAcceso: "ABC123",
            operadorCreadorId: Operador,
            fechaCreacion: AhoraUtc,
            fechaInicioUtc: null,
            fechaFinalizacionUtc: null,
            misiones: new[] { SesionMision.Rehidratar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1) });
    }

    private static SesionGrupal CrearGrupal(string nombre, EstadoSesion estado)
    {
        return SesionGrupal.Rehidratar(
            id: Guid.NewGuid(),
            nombre: nombre,
            descripcion: "Demo",
            estado: estado,
            fechaProgramada: AhoraUtc.AddHours(2),
            codigoAcceso: "DEF456",
            operadorCreadorId: Operador,
            fechaCreacion: AhoraUtc,
            fechaInicioUtc: null,
            fechaFinalizacionUtc: null,
            misiones: new[] { SesionMision.Rehidratar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1) });
    }

    [Fact]
    public async Task Handle_FiltroTodas_SeNormalizaANullEnElRepositorio()
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ListarDisponiblesParaParticipanteAsync(
                "piloto", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>())
            .Verifiable();

        var manejador = new ListarSesionesDisponiblesParticipanteManejador(repo.Object);

        await manejador.Handle(
            new ListarSesionesDisponiblesParticipanteConsulta("piloto", "Todas"),
            CancellationToken.None);

        repo.Verify();
    }

    [Fact]
    public async Task Handle_FiltroIndividual_LoPropagaTalCual()
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ListarDisponiblesParaParticipanteAsync(
                null, "Individual", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>())
            .Verifiable();

        var manejador = new ListarSesionesDisponiblesParticipanteManejador(repo.Object);

        await manejador.Handle(
            new ListarSesionesDisponiblesParticipanteConsulta(null, "Individual"),
            CancellationToken.None);

        repo.Verify();
    }

    [Fact]
    public async Task Handle_SesionIndividual_ExponeCapacidadDeParticipantes()
    {
        var individual = CrearIndividual("Piloto", EstadoSesion.Programada);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ListarDisponiblesParaParticipanteAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { individual });

        var manejador = new ListarSesionesDisponiblesParticipanteManejador(repo.Object);

        var resultado = await manejador.Handle(
            new ListarSesionesDisponiblesParticipanteConsulta(null, null),
            CancellationToken.None);

        var dto = resultado.Single();
        dto.Modo.Should().Be("Individual");
        dto.CapacidadMaximaParticipantes.Should().Be(
            PoliticaCapacidadSesion.MaximoParticipantesIndividual);
        dto.CapacidadMaximaEquipos.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SesionGrupal_ExponeCapacidadDeEquipos()
    {
        var grupal = CrearGrupal("Piloto grupal", EstadoSesion.Activa);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ListarDisponiblesParaParticipanteAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { grupal });

        var manejador = new ListarSesionesDisponiblesParticipanteManejador(repo.Object);

        var resultado = await manejador.Handle(
            new ListarSesionesDisponiblesParticipanteConsulta(null, null),
            CancellationToken.None);

        var dto = resultado.Single();
        dto.Modo.Should().Be("Grupal");
        dto.CapacidadMaximaEquipos.Should().Be(
            PoliticaCapacidadSesion.MaximoEquiposPorSesion);
        dto.CapacidadMaximaParticipantes.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DtoNoExponeOperadorCreadorIdNiDatosAdministrativos()
    {
        var sesion = CrearIndividual("Piloto", EstadoSesion.Programada);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ListarDisponiblesParaParticipanteAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { sesion });

        var manejador = new ListarSesionesDisponiblesParticipanteManejador(repo.Object);

        var resultado = await manejador.Handle(
            new ListarSesionesDisponiblesParticipanteConsulta(null, null),
            CancellationToken.None);

        var dto = resultado.Single();
        var propiedades = dto.GetType().GetProperties().Select(p => p.Name).ToList();
        propiedades.Should().NotContain("OperadorCreadorId");
        propiedades.Should().NotContain("CodigoAcceso");
        propiedades.Should().NotContain("FechaCreacion");
        propiedades.Should().NotContain("FechaInicioUtc");
        propiedades.Should().NotContain("FechaFinalizacionUtc");
    }
}
