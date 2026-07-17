using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;
using Xunit;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

// HU52 — Persistencia de PenalizacionSesion (mapper dominio↔modelo, consulta por
// EventoId y actualización del estado de procesamiento).
public sealed class RepositorioPenalizacionesSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Agregar_yObtenerPorEventoId_rehidrataLaEntidad()
    {
        var opciones = OpcionesInMemory();
        var eventoId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesSesion(contexto);
            var penalizacion = PenalizacionSesion.CrearParaEquipo(
                eventoId, sesionId, equipoId, 10, "Motivo del equipo", Operador, AhoraUtc);
            await repo.AgregarAsync(penalizacion, CancellationToken.None);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesSesion(contexto);
            var recuperada = await repo.ObtenerPorEventoIdAsync(eventoId, CancellationToken.None);

            recuperada.Should().NotBeNull();
            recuperada!.EventoId.Should().Be(eventoId);
            recuperada.SesionId.Should().Be(sesionId);
            recuperada.EquipoId.Should().Be(equipoId);
            recuperada.ParticipanteSesionId.Should().BeNull();
            recuperada.TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Equipo);
            recuperada.Puntos.Should().Be(10);
            recuperada.Motivo.Should().Be("Motivo del equipo");
            recuperada.EstadoProcesamiento.Should().Be(EstadoProcesamientoPenalizacion.Pendiente);
        }
    }

    [Fact]
    public async Task Actualizar_marcarProcesada_persisteEstadoYResultado()
    {
        var opciones = OpcionesInMemory();
        var eventoId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesSesion(contexto);
            var penalizacion = PenalizacionSesion.CrearParaParticipante(
                eventoId, Guid.NewGuid(), participanteSesionId, Guid.NewGuid(),
                5, "Motivo", Operador, AhoraUtc);
            await repo.AgregarAsync(penalizacion, CancellationToken.None);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesSesion(contexto);
            var penalizacion = await repo.ObtenerPorEventoIdAsync(eventoId, CancellationToken.None);
            penalizacion!.MarcarProcesada(-3, AhoraUtc.AddMinutes(1));
            await repo.ActualizarAsync(penalizacion, CancellationToken.None);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesSesion(contexto);
            var penalizacion = await repo.ObtenerPorEventoIdAsync(eventoId, CancellationToken.None);
            penalizacion!.EstadoProcesamiento.Should().Be(EstadoProcesamientoPenalizacion.Procesada);
            penalizacion.PuntajeResultante.Should().Be(-3);
            penalizacion.ProcesadaEnUtc.Should().Be(AhoraUtc.AddMinutes(1));
        }
    }

    [Fact]
    public async Task ObtenerPorEventoId_inexistente_devuelveNull()
    {
        await using var contexto = new ContextoSesiones(OpcionesInMemory());
        var repo = new RepositorioPenalizacionesSesion(contexto);

        (await repo.ObtenerPorEventoIdAsync(Guid.NewGuid(), CancellationToken.None))
            .Should().BeNull();
    }

    private static DbContextOptions<ContextoSesiones> OpcionesInMemory()
        => new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase("penalizaciones-" + Guid.NewGuid())
            .Options;
}
