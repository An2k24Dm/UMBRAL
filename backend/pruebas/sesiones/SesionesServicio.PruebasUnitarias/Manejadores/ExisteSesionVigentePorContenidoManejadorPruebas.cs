using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.CasosDeUso.Manejadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU33 — Manejador delgado: sólo verifica que reenvía los argumentos
// al repositorio y devuelve el booleano envuelto en el DTO. La lógica
// "qué cuenta como vigente" se prueba contra el repositorio real con
// InMemory en RepositorioSesionesExisteSesionVigentePruebas.
public class ExisteSesionVigentePorContenidoManejadorPruebas
{
    private static readonly Guid ContenidoJuegoId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_DevuelveDtoConElValorDelRepositorio(bool esperado)
    {
        var repositorio = new Mock<IRepositorioSesiones>();
        repositorio
            .Setup(r => r.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.Trivia, ContenidoJuegoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var manejador = new ExisteSesionVigentePorContenidoManejador(repositorio.Object);

        var respuesta = await manejador.Handle(
            new ExisteSesionVigentePorContenidoConsulta(TipoJuego.Trivia, ContenidoJuegoId),
            CancellationToken.None);

        respuesta.Existe.Should().Be(esperado);
        repositorio.Verify(r => r.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, ContenidoJuegoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagaTipoJuegoBusquedaTesoroAlRepositorio()
    {
        var repositorio = new Mock<IRepositorioSesiones>();
        repositorio
            .Setup(r => r.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.BusquedaTesoro, ContenidoJuegoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var manejador = new ExisteSesionVigentePorContenidoManejador(repositorio.Object);

        await manejador.Handle(
            new ExisteSesionVigentePorContenidoConsulta(TipoJuego.BusquedaTesoro, ContenidoJuegoId),
            CancellationToken.None);

        repositorio.Verify(r => r.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.BusquedaTesoro, ContenidoJuegoId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
