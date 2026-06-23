using JuegosServicio.Aplicacion.Consultas.ObtenerDetalleMisionParticipante;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// Reglas del manejador móvil:
//   * Misión inexistente → null (controlador devuelve 404).
//   * Misión existente pero en estado distinto de "Activa" → null
//     (defensa en profundidad: el Participante no debe ver borradores).
//   * Misión Activa → DTO recortado con etapas ordenadas por Orden.
//   * El DTO NO expone creadorId, FechaCreacion ni TiempoTotal (datos
//     administrativos del flujo Operador).
public class ObtenerDetalleMisionParticipanteManejadorPruebas
{
    private readonly Mock<IRepositorioMisiones> _repositorio = new();

    private ObtenerDetalleMisionParticipanteManejador CrearManejador() =>
        new(_repositorio.Object);

    private static MisionDetalleDto Detalle(Guid id, string estado, IEnumerable<EtapaDetalleDto>? etapas = null)
        => new()
        {
            Id = id,
            Nombre = "Misión piloto",
            Descripcion = "Descripción",
            Estado = estado,
            Dificultad = "Media",
            FechaCreacion = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            TiempoTotal = 600,
            Etapas = etapas?.ToList() ?? new List<EtapaDetalleDto>()
        };

    [Fact]
    public async Task Handle_MisionInexistente_RetornaNull()
    {
        var misionId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerDetalleMisionAsync(misionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MisionDetalleDto?)null);

        var resultado = await CrearManejador().Handle(
            new ObtenerDetalleMisionParticipanteConsulta(misionId),
            CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MisionNoActiva_RetornaNull()
    {
        var misionId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerDetalleMisionAsync(misionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Detalle(misionId, estado: "Inactiva"));

        var resultado = await CrearManejador().Handle(
            new ObtenerDetalleMisionParticipanteConsulta(misionId),
            CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MisionActiva_DevuelveDtoConEtapasOrdenadas()
    {
        var misionId = Guid.NewGuid();
        var etapaA = new EtapaDetalleDto
        {
            Id = Guid.NewGuid(), Orden = 2, TipoModoDeJuego = "Trivia",
            ModoDeJuegoId = Guid.NewGuid(), NombreModoDeJuego = "Trivia A",
            TiempoEstimado = 120
        };
        var etapaB = new EtapaDetalleDto
        {
            Id = Guid.NewGuid(), Orden = 1, TipoModoDeJuego = "BusquedaDelTesoro",
            ModoDeJuegoId = Guid.NewGuid(), NombreModoDeJuego = "Búsqueda B",
            TiempoEstimado = 300
        };
        _repositorio
            .Setup(r => r.ObtenerDetalleMisionAsync(misionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Detalle(misionId, "Activa", new[] { etapaA, etapaB }));

        var resultado = await CrearManejador().Handle(
            new ObtenerDetalleMisionParticipanteConsulta(misionId),
            CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(misionId);
        resultado.Estado.Should().Be("Activa");
        resultado.Etapas.Should().HaveCount(2);
        resultado.Etapas[0].Orden.Should().Be(1);
        resultado.Etapas[1].Orden.Should().Be(2);
        resultado.Etapas[0].NombreModoDeJuego.Should().Be("Búsqueda B");
    }

    [Fact]
    public void Dto_NoExponeCamposAdministrativos()
    {
        var propiedades = typeof(MisionDetalleParticipanteDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        propiedades.Should().NotContain("CreadorId");
        propiedades.Should().NotContain("FechaCreacion");
        propiedades.Should().NotContain("TiempoTotal");
    }
}
