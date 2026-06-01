using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.ServiciosEnSegundoPlano;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.ServiciosEnSegundoPlano;

// HU34/5.1 — Cubre la transición automática Programada → EnPreparacion.
public class ProcesadorPreparacionSesionesPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Sesion Sesion(EstadoSesion estado, DateTime? fechaProgramada = null)
        => SesionesServicio.Dominio.Entidades.Sesion.Rehidratar(
            Guid.NewGuid(),
            "Sesión",
            TipoJuego.Trivia,
            Guid.NewGuid(),
            ModoSesion.Individual,
            estado,
            fechaProgramada ?? AhoraUtc.AddMinutes(-5),
            Admin,
            AhoraUtc);

    private static (ProcesadorPreparacionSesiones procesador,
                    Mock<IRepositorioSesiones> repo,
                    Mock<IUnidadTrabajoSesiones> unidad,
                    Mock<IProveedorFechaHora> reloj)
        CrearProcesador()
    {
        var repo = new Mock<IRepositorioSesiones>();
        var unidad = new Mock<IUnidadTrabajoSesiones>();
        var reloj = new Mock<IProveedorFechaHora>();
        reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
        var procesador = new ProcesadorPreparacionSesiones(
            repo.Object, unidad.Object, reloj.Object,
            NullLogger<ProcesadorPreparacionSesiones>.Instance);
        return (procesador, repo, unidad, reloj);
    }

    [Fact]
    public async Task SesionProgramadaVencida_DebePasarA_EnPreparacion()
    {
        var (procesador, repo, unidad, _) = CrearProcesador();
        var sesion = Sesion(EstadoSesion.Programada, AhoraUtc.AddMinutes(-5));
        repo.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { sesion });

        Sesion? actualizada = null;
        repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
            .Callback<Sesion, CancellationToken>((s, _) => actualizada = s)
            .Returns(Task.CompletedTask);

        var resultado = await procesador.EjecutarCicloAsync(CancellationToken.None);

        resultado.Encontradas.Should().Be(1);
        resultado.Preparadas.Should().Be(1);
        actualizada.Should().NotBeNull();
        actualizada!.Estado.Should().Be(EstadoSesion.EnPreparacion);
        unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoHaySesionesVencidas_NoDebeGuardarCambios()
    {
        var (procesador, repo, unidad, _) = CrearProcesador();
        repo.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>());

        var resultado = await procesador.EjecutarCicloAsync(CancellationToken.None);

        resultado.Encontradas.Should().Be(0);
        resultado.Preparadas.Should().Be(0);
        unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task SesionConOtroEstado_NoDebeSerProcesada(EstadoSesion estado)
    {
        // Defensa en profundidad: aunque el repositorio devuelve sólo
        // Programadas, si por concurrencia algo cambió, el procesador
        // debe detectarlo y no llamar a Preparar(). Simulamos que el
        // repo devolvió una sesión que YA no está en Programada.
        var (procesador, repo, _, _) = CrearProcesador();
        repo.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { Sesion(estado) });

        var resultado = await procesador.EjecutarCicloAsync(CancellationToken.None);

        resultado.Preparadas.Should().Be(0);
        repo.Verify(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UsaProveedorFechaHora_NoUtcNow()
    {
        var (procesador, repo, _, reloj) = CrearProcesador();
        repo.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>());

        await procesador.EjecutarCicloAsync(CancellationToken.None);

        reloj.Verify(r => r.ObtenerFechaHoraUtc(), Times.AtLeastOnce);
        repo.Verify(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
