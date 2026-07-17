using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;
using SesionesServicio.Infraestructura.ServiciosExternos;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

public sealed class UnidadTrabajoSesionesPruebas
{
    [Fact]
    public async Task EjecutarEnTransaccion_GuardaCambiosPendientesAntesDeConfirmar()
    {
        var nombreBase = "unidad-trabajo-sesiones-" + Guid.NewGuid();
        var raiz = new InMemoryDatabaseRoot();
        var eventoId = Guid.NewGuid();

        await using (var contexto = NuevoContexto(nombreBase, raiz))
        {
            var unidadTrabajo = new UnidadTrabajoSesiones(contexto);

            await unidadTrabajo.EjecutarEnTransaccionAsync(ct =>
            {
                contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
                {
                    Id = eventoId,
                    RoutingKey = "sesion.respuesta_trivia",
                    PayloadJson = "{}",
                    CreadoEnUtc = DateTime.UtcNow,
                    Estado = "Pendiente"
                });

                return Task.CompletedTask;
            }, CancellationToken.None);
        }

        await using var verificacion = NuevoContexto(nombreBase, raiz);
        var existe = await verificacion.OutboxRanking
            .AnyAsync(m => m.Id == eventoId, CancellationToken.None);

        existe.Should().BeTrue();
    }

    private static ContextoSesiones NuevoContexto(
        string nombreBase,
        InMemoryDatabaseRoot raiz)
    {
        var opciones = new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase(nombreBase, raiz)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ContextoSesiones(opciones);
    }
}
