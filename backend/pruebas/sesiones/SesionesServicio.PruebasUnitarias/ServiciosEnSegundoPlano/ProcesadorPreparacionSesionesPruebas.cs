using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Procesos.PreparacionSesiones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.ServiciosEnSegundoPlano;

// Transición automática Programada → EnPreparacion. Rehidratamos
// SesionIndividual con la nueva firma para reflejar el modelo abstracto.
public class ProcesadorPreparacionSesionesPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Sesion CrearSesion(EstadoSesion estado, DateTime? fechaProgramada = null)
        => SesionIndividual.Rehidratar(
            id: Guid.NewGuid(),
            nombre: "Sesión",
            descripcion: "Demo",
            estado: estado,
            fechaProgramada: fechaProgramada ?? AhoraUtc.AddMinutes(-5),
            codigoAcceso: "ABC123",
            operadorCreadorId: Operador,
            fechaCreacion: AhoraUtc,
            fechaInicioUtc: null,
            fechaFinalizacionUtc: null,
            maximoParticipantes: 10);

    private static (ProcesadorPreparacionSesiones procesador,
                    Mock<IConsultasSesiones> consultas,
                    Mock<IRepositorioSesiones> repo,
                    Mock<IUnidadTrabajoSesiones> unidad,
                    Mock<IProveedorFechaHora> reloj)
        CrearProcesador()
    {
        var consultas = new Mock<IConsultasSesiones>();
        var repo = new Mock<IRepositorioSesiones>();
        var unidad = new Mock<IUnidadTrabajoSesiones>();
        var reloj = new Mock<IProveedorFechaHora>();
        reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
        var procesador = new ProcesadorPreparacionSesiones(
            consultas.Object, repo.Object, unidad.Object, reloj.Object,
            Mock.Of<IRegistroLogsAplicacion>());
        return (procesador, consultas, repo, unidad, reloj);
    }

    [Fact]
    public async Task SesionProgramadaVencida_DebePasarA_EnPreparacion()
    {
        var (procesador, consultas, repo, unidad, _) = CrearProcesador();
        var sesion = CrearSesion(EstadoSesion.Programada, AhoraUtc.AddMinutes(-5));
        consultas.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { sesion });

        Sesion? actualizada = null;
        repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
            .Callback<Sesion, CancellationToken>((s, _) => actualizada = s)
            .Returns(Task.CompletedTask);

        var resultado = await procesador.EjecutarCicloAsync(CancellationToken.None);

        resultado.Encontradas.Should().Be(1);
        resultado.Preparadas.Should().Be(1);
        actualizada!.Estado.Should().Be(EstadoSesion.EnPreparacion);
        unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoHaySesionesVencidas_NoDebeGuardarCambios()
    {
        var (procesador, consultas, _, unidad, _) = CrearProcesador();
        consultas.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion>());

        var resultado = await procesador.EjecutarCicloAsync(CancellationToken.None);

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
        var (procesador, consultas, repo, _, _) = CrearProcesador();
        consultas.Setup(r => r.ListarProgramadasVencidasAsync(AhoraUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sesion> { CrearSesion(estado) });

        var resultado = await procesador.EjecutarCicloAsync(CancellationToken.None);

        resultado.Preparadas.Should().Be(0);
        repo.Verify(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
