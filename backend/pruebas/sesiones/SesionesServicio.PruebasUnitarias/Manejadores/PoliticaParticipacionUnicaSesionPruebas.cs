using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// RB — Participación única: un participante solo puede estar en una sesión
// (EnPreparacion, Activa o Pausada) a la vez.
public class PoliticaParticipacionUnicaSesionPruebas
{
    private static readonly Guid Participante = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SesionDestino = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtraSesion = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static (PoliticaParticipacionUnicaSesion, Mock<IConsultasSesiones>) Construir(
        SesionParticipacionActivaDto? activa)
    {
        var consultas = new Mock<IConsultasSesiones>();
        consultas.Setup(c => c.ObtenerParticipacionActivaDeParticipanteAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activa);
        return (new PoliticaParticipacionUnicaSesion(consultas.Object), consultas);
    }

    private static SesionParticipacionActivaDto Participacion(
        Guid sesionId, EstadoSesion estado, ModoSesion modo = ModoSesion.Grupal)
        => new(sesionId, "Sesión", estado, modo, null, null);

    [Fact]
    public async Task Permite_SiNoTieneParticipacionActiva()
    {
        var (validador, _) = Construir(null);

        Func<Task> accion = () => validador.ValidarPuedeIngresarASesionAsync(
            Participante, SesionDestino, CancellationToken.None);

        await accion.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task Bloquea_SiEstaEnOtraSesion(EstadoSesion estado)
    {
        var (validador, _) = Construir(Participacion(OtraSesion, estado));

        Func<Task> accion = () => validador.ValidarPuedeIngresarASesionAsync(
            Participante, SesionDestino, CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipanteYaEstaEnSesionActivaExcepcion>();
    }

    [Fact]
    public async Task Bloquea_SiYaPerteneceALaMismaSesion()
    {
        var (validador, _) = Construir(
            Participacion(SesionDestino, EstadoSesion.EnPreparacion));

        Func<Task> accion = () => validador.ValidarPuedeIngresarASesionAsync(
            Participante, SesionDestino, CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipanteYaPerteneceASesionExcepcion>();
    }

    [Fact]
    public async Task Funciona_ParaParticipacionIndividual()
    {
        var (validador, _) = Construir(
            Participacion(OtraSesion, EstadoSesion.Activa, ModoSesion.Individual));

        Func<Task> accion = () => validador.ValidarPuedeIngresarASesionAsync(
            Participante, SesionDestino, CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipanteYaEstaEnSesionActivaExcepcion>();
    }
}
