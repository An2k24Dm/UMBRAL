using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)

namespace SesionesServicio.PruebasUnitarias.Consultas;

// HU52 — El desglose diferencia puntaje bruto, penalizaciones acumuladas y
// puntaje total autoritativo (individual: participante; grupal: equipo).
public sealed class ObtenerMiDesgloseSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IRepositorioRespuestasTrivia> Trivia { get; } = new();
        public Mock<IRepositorioEvidenciasTesoro> Tesoro { get; } = new();
        public Mock<IRepositorioPenalizacionesAplicadas> Penalizaciones { get; } = new();
        public Mock<IClienteJuegosMisiones> Misiones { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();

        public Contexto(Sesion sesion, Guid identidadId)
        {
            Usuario.Setup(u => u.ObtenerId()).Returns(identidadId);
            Repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Trivia.Setup(t => t.ObtenerPuntajePorEtapaParticipanteAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PuntajeEtapaItem>().AsReadOnly());
            Tesoro.Setup(t => t.ObtenerPuntajePorEtapaParticipanteAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PuntajeEtapaItem>().AsReadOnly());
            Trivia.Setup(t => t.ObtenerPuntajeGanadoEquipoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0L);
            Tesoro.Setup(t => t.ObtenerPuntajeGanadoEquipoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0L);
            Penalizaciones.Setup(p => p.SumarPuntosPorParticipanteAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            Penalizaciones.Setup(p => p.SumarPuntosPorEquipoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            Misiones.Setup(m => m.ObtenerMisionConEtapasAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SesionesServicio.Commons.Dtos.MisionConEtapasJuegosDto?)null);
        }

        public ObtenerMiDesgloseSesionManejador Construir()
            => new(Repo.Object, Penalizaciones.Object, Trivia.Object, Tesoro.Object, Misiones.Object, Usuario.Object);
    }

    [Fact]
    public async Task Individual_puntajeTotalEsBrutoMenosPenalizacionDelParticipante()
    {
        var identidadId = Guid.NewGuid();
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        var misionId = Guid.NewGuid();
        sesion.AsignarMisiones(new[] { misionId });
        sesion.Preparar();
        var participante = sesion.AgregarParticipante(identidadId, AhoraUtc);
        sesion.Iniciar(AhoraUtc);
        participante.EstablecerPenalizacionSnapshot(52, 6, AhoraUtc);

        var ctx = new Contexto(sesion, identidadId);
        ctx.Penalizaciones.Setup(p => p.SumarPuntosPorParticipanteAsync(
                sesion.Id, identidadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(52);
        var etapaId = Guid.NewGuid();
        ctx.Trivia.Setup(t => t.ObtenerPuntajePorEtapaParticipanteAsync(
                sesion.Id, identidadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PuntajeEtapaItem> { new(misionId, etapaId, 58) }.AsReadOnly());

        var dto = await ctx.Construir().Handle(
            new ObtenerMiDesgloseSesionConsulta(sesion.Id), CancellationToken.None);

        dto.PuntajeBruto.Should().Be(58);
        dto.PuntosPenalizados.Should().Be(52);
        dto.PuntajeTotal.Should().Be(6);
    }

    [Fact]
    public async Task Grupal_puntajeTotalEsBrutoEquipoMenosPenalizacionDelEquipo()
    {
        var identidadId = Guid.NewGuid();
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
        var misionId = Guid.NewGuid();
        sesion.AsignarMisiones(new[] { misionId });
        sesion.Preparar();
        var equipo = sesion.CrearEquipo("Rojo", identidadId, AhoraUtc, AhoraUtc);
        sesion.Iniciar(AhoraUtc);
        equipo.EstablecerPenalizacionSnapshot(20, 60, AhoraUtc);

        var ctx = new Contexto(sesion, identidadId);
        ctx.Penalizaciones.Setup(p => p.SumarPuntosPorEquipoAsync(
                sesion.Id, equipo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);
        // Puntaje bruto propio del participante (misiones/etapas).
        ctx.Trivia.Setup(t => t.ObtenerPuntajePorEtapaParticipanteAsync(
                sesion.Id, identidadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PuntajeEtapaItem> { new(misionId, Guid.NewGuid(), 30) }.AsReadOnly());
        // Puntaje bruto del equipo completo (todos sus integrantes).
        ctx.Trivia.Setup(t => t.ObtenerPuntajeGanadoEquipoAsync(
                sesion.Id, equipo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(80L);

        var dto = await ctx.Construir().Handle(
            new ObtenerMiDesgloseSesionConsulta(sesion.Id), CancellationToken.None);

        dto.PuntajeBruto.Should().Be(30);       // aporte propio
        dto.PuntosPenalizados.Should().Be(20);  // penalización del equipo
        dto.PuntajeTotal.Should().Be(60);       // 80 (equipo) − 20
    }

    [Fact]
    public async Task SinIdentidad_devuelveDesgloseVacio()
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        var ctx = new Contexto(sesion, Guid.Empty);

        var dto = await ctx.Construir().Handle(
            new ObtenerMiDesgloseSesionConsulta(sesion.Id), CancellationToken.None);

        dto.PuntajeBruto.Should().Be(0);
        dto.PuntosPenalizados.Should().Be(0);
        dto.PuntajeTotal.Should().Be(0);
    }
}
