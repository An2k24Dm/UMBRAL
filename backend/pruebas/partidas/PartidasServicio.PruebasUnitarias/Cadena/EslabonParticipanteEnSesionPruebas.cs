using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.PruebasUnitarias.Cadena;

public class EslabonParticipanteEnSesionPruebas
{
    private static EslabonParticipanteEnSesion CrearEslabon() => new();

    private static ContextoValidacionRespuesta ContextoInscrito(Guid? equipoId = null) => new()
    {
        SesionId = Guid.NewGuid(),
        PreguntaId = Guid.NewGuid(),
        ParticipanteId = Guid.NewGuid(),
        ParticipanteInscrito = true,
        EquipoId = equipoId
    };

    private static ContextoValidacionRespuesta ContextoNoInscrito() => new()
    {
        SesionId = Guid.NewGuid(),
        PreguntaId = Guid.NewGuid(),
        ParticipanteId = Guid.NewGuid(),
        ParticipanteInscrito = false
    };

    [Fact]
    public async Task ValidarAsync_ParticipanteInscrito_NoLanzaExcepcion()
    {
        var accion = async () =>
            await CrearEslabon().ValidarAsync(ContextoInscrito(), CancellationToken.None);

        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidarAsync_ParticipanteInscritoEnEquipo_NoLanzaExcepcion()
    {
        var accion = async () =>
            await CrearEslabon().ValidarAsync(ContextoInscrito(Guid.NewGuid()), CancellationToken.None);

        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidarAsync_ParticipanteNoInscrito_LanzaParticipanteNoEnSesionExcepcion()
    {
        var accion = async () =>
            await CrearEslabon().ValidarAsync(ContextoNoInscrito(), CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipanteNoEnSesionExcepcion>();
    }

    [Fact]
    public async Task ValidarAsync_NoModificaContexto()
    {
        var equipoId = Guid.NewGuid();
        var contexto = ContextoInscrito(equipoId);

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        contexto.EquipoId.Should().Be(equipoId);
        contexto.ParticipanteInscrito.Should().BeTrue();
    }
}
