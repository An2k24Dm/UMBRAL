using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerBusquedaTesoroConPistas;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers

namespace SesionesServicio.PruebasUnitarias.Consultas;

public class ObtenerBusquedaTesoroConPistasManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Participante = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Ajeno = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid MisionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EtapaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid BusquedaId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private sealed class Arranque
    {
        public Mock<IClienteBusquedaTesoro> ClienteTesoro { get; } = new();
        public Mock<IRepositorioPistasLiberadas> Pistas { get; } = new();
        public Mock<IRepositorioEvidenciasTesoro> Evidencias { get; } = new();
        public Mock<IRepositorioSesiones> Sesiones { get; } = new();
        public Mock<IServicioProgresoSecuencialSesion> Progreso { get; } = new();

        public Arranque(Sesion sesion)
        {
            Sesiones.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Progreso.Setup(p => p.ValidarEtapaActualAsync(
                    It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            ClienteTesoro.Setup(c => c.ObtenerBusquedaParticipanteAsync(
                    BusquedaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BusquedaTesoroJuegosDto
                {
                    Id = BusquedaId, Nombre = "Tesoro", Descripcion = "Encuentra el QR",
                    Tiempo = 5, Puntaje = 100
                });
            Pistas.Setup(r => r.ObtenerPorEtapaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            Evidencias.Setup(r => r.ExisteEvidenciaValidaIndividualAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        public ObtenerBusquedaTesoroConPistasManejador Construir()
            => new(ClienteTesoro.Object, Pistas.Object, Evidencias.Object,
                   Sesiones.Object, Progreso.Object);

        public Task<BusquedaTesoroConPistasDto?> EjecutarAsync(Guid sesionId, Guid participante)
            => Construir().Handle(
                new ObtenerBusquedaTesoroConPistasConsulta(sesionId, MisionId, EtapaId, BusquedaId, participante),
                CancellationToken.None);
    }

    private static SesionIndividual Individual()
    {
        var s = SesionIndividual.Crear("Tesoro", "Demo", AhoraUtc.AddHours(1), "COD001", Operador, AhoraUtc, 5);
        s.Preparar();
        s.AgregarParticipante(Participante, AhoraUtc);
        s.Iniciar(AhoraUtc);
        return s;
    }

    private static SesionGrupal GrupalSinEsteParticipante()
    {
        var s = SesionGrupal.Crear("Tesoro", "Demo", AhoraUtc.AddHours(1), "COD002", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);
        s.Preparar();
        s.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc); // otro participante
        s.Iniciar(AhoraUtc);
        return s;
    }

    [Fact]
    public async Task BusquedaDeCincoMinutos_DevuelveTiempoSegundosEnTrescientos()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);

        var resultado = await arr.EjecutarAsync(sesion.Id, Participante);

        resultado.Should().NotBeNull();
        resultado!.TiempoSegundos.Should().Be(300);
    }

    [Fact] // (#2/#3) Si la etapa no es la actual, la lectura se bloquea y no se consulta contenido.
    public async Task EtapaNoActual_BloqueaLecturaDeContenido()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);
        arr.Progreso.Setup(p => p.ValidarEtapaActualAsync(
                It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperacionSesionInvalidaExcepcion(
                "Debes completar la etapa anterior antes de jugar esta etapa."));

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id, Participante);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
        arr.ClienteTesoro.Verify(c => c.ObtenerBusquedaParticipanteAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (#4) Grupal: un participante sin equipo NO se trata como individual; se rechaza.
    public async Task GrupalSinEquipo_LanzaParticipacionInvalida()
    {
        var sesion = GrupalSinEsteParticipante();
        var arr = new Arranque(sesion);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id, Ajeno);

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_LanzaSesionNoEncontrada()
    {
        var sesion = Individual();
        var arr = new Arranque(sesion);
        arr.Sesiones.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => arr.EjecutarAsync(Guid.NewGuid(), Participante);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }
}
