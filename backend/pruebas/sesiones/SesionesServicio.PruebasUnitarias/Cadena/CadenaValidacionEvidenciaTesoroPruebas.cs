using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)
using Xunit;

namespace SesionesServicio.PruebasUnitarias.Cadena;

// Pruebas del patrón Chain of Responsibility para la validación de evidencias de
// Búsqueda del Tesoro. Cubren: cada eslabón detiene la cadena, el orden de
// ejecución, el cortocircuito ante un fallo, y la semántica del QR (incorrecto
// = resultado false sin excepción).
public sealed class CadenaValidacionEvidenciaTesoroPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Otro = Guid.Parse("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a");
    private static readonly Guid Ana = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid BusquedaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string Codigo = "QR-TESORO-001";

    // ---------------------------------------------------------------- escenario

    private sealed class Escenario
    {
        public Mock<IRepositorioSesiones> RepoSesiones { get; } = new();
        public Mock<IServicioProgresoSecuencialSesion> Progreso { get; } = new();
        public Mock<IRepositorioEvidenciasTesoro> RepoEvidencias { get; } = new();
        public Mock<IClienteBusquedaTesoro> Cliente { get; } = new();

        public Escenario(Sesion? sesion, bool duplicado = false, bool? qrValido = true)
        {
            RepoSesiones.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Progreso.Setup(p => p.ValidarEtapaActualAsync(
                    It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            RepoEvidencias.Setup(r => r.ExisteEvidenciaValidaIndividualAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(duplicado);
            RepoEvidencias.Setup(r => r.ExisteEvidenciaValidaEquipoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(duplicado);
            Cliente.Setup(c => c.ValidarCodigoQrAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(qrValido);
        }

        public IEslabonValidacionEvidenciaTesoro Cadena()
            => new FabricaCadenaValidacionEvidenciaTesoro(
                new EslabonSesionActiva(RepoSesiones.Object),
                new EslabonParticipanteInscrito(),
                new EslabonEtapaActual(Progreso.Object),
                new EslabonEvidenciaNoDuplicada(RepoEvidencias.Object),
                new EslabonCodigoQr(Cliente.Object)).Crear();
    }

    private static ContextoValidacionEvidenciaTesoro Contexto(Guid participanteId)
        => new()
        {
            SesionId = Guid.NewGuid(),
            ParticipanteIdentidadId = participanteId,
            MisionId = MisionId,
            EtapaId = EtapaId,
            BusquedaId = BusquedaId,
            CodigoEscaneado = Codigo
        };

    private static SesionIndividual IndividualActiva()
    {
        var s = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new[] { MisionId });
        s.Preparar();
        s.AgregarParticipante(ParticipanteId, AhoraUtc);
        s.IniciarPrimeraEtapa(MisionId, EtapaId, BusquedaId, "BusquedaTesoro", 1, AhoraUtc, 300);
        return s;
    }

    private static (SesionGrupal sesion, Guid equipoId) GrupalActiva()
    {
        var s = SesionGrupal.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);
        s.AsignarMisiones(new[] { MisionId });
        s.Preparar();
        var rojo = s.CrearEquipo("Rojo", Ana, AhoraUtc, AhoraUtc);
        s.IniciarPrimeraEtapa(MisionId, EtapaId, BusquedaId, "BusquedaTesoro", 1, AhoraUtc, 300);
        return (s, rojo.Id);
    }

    // ---------------------------------------------------------------- A..H (cadena real)

    [Fact] // (A) Sesión inexistente: primer eslabón lanza; los demás no se ejecutan.
    public async Task SesionInexistente_DetieneCadena()
    {
        var esc = new Escenario(sesion: null);
        Func<Task> accion = () => esc.Cadena().ManejarAsync(Contexto(ParticipanteId), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
        esc.Progreso.Verify(p => p.ValidarEtapaActualAsync(
            It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        esc.Cliente.Verify(c => c.ValidarCodigoQrAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (B) Sesión no activa: la cadena se detiene en el primer eslabón.
    public async Task SesionNoActiva_DetieneCadena()
    {
        var s = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc, 5);
        s.Preparar(); // EnPreparacion, no Activa
        var esc = new Escenario(s);

        Func<Task> accion = () => esc.Cadena().ManejarAsync(Contexto(ParticipanteId), CancellationToken.None);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
        esc.Cliente.Verify(c => c.ValidarCodigoQrAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (C) Participante no inscrito: la cadena se detiene.
    public async Task ParticipanteNoInscrito_DetieneCadena()
    {
        var esc = new Escenario(IndividualActiva());

        Func<Task> accion = () => esc.Cadena().ManejarAsync(Contexto(Otro), CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
        esc.Cliente.Verify(c => c.ValidarCodigoQrAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (D) Etapa incorrecta: el servicio de progreso lanza y la cadena se detiene.
    public async Task EtapaIncorrecta_DetieneCadena()
    {
        var esc = new Escenario(IndividualActiva());
        esc.Progreso.Setup(p => p.ValidarEtapaActualAsync(
                It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("etapa incorrecta"));

        Func<Task> accion = () => esc.Cadena().ManejarAsync(Contexto(ParticipanteId), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        esc.RepoEvidencias.Verify(r => r.ExisteEvidenciaValidaIndividualAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        esc.Cliente.Verify(c => c.ValidarCodigoQrAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (E) Evidencia válida duplicada individual: la cadena se detiene, sin validar QR.
    public async Task DuplicadaIndividual_DetieneCadena()
    {
        var esc = new Escenario(IndividualActiva(), duplicado: true);

        Func<Task> accion = () => esc.Cadena().ManejarAsync(Contexto(ParticipanteId), CancellationToken.None);

        (await accion.Should().ThrowAsync<EvidenciaTesoroDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeFalse();
        esc.Cliente.Verify(c => c.ValidarCodigoQrAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (F) Evidencia válida duplicada grupal: se detiene para cualquier miembro del equipo.
    public async Task DuplicadaGrupal_DetieneCadena()
    {
        var (sesion, _) = GrupalActiva();
        var esc = new Escenario(sesion, duplicado: true);

        Func<Task> accion = () => esc.Cadena().ManejarAsync(Contexto(Ana), CancellationToken.None);

        (await accion.Should().ThrowAsync<EvidenciaTesoroDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeTrue();
        // Usa el conteo por equipo, no el individual.
        esc.RepoEvidencias.Verify(r => r.ExisteEvidenciaValidaEquipoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        esc.RepoEvidencias.Verify(r => r.ExisteEvidenciaValidaIndividualAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (G) QR inválido: la cadena termina bien, EsCodigoQrValido == false, sin excepción.
    public async Task QrInvalido_TerminaCadenaSinExcepcion()
    {
        var esc = new Escenario(IndividualActiva(), qrValido: false);
        var contexto = Contexto(ParticipanteId);

        await esc.Cadena().ManejarAsync(contexto, CancellationToken.None);

        contexto.EsCodigoQrValido.Should().BeFalse();
    }

    [Fact] // (H) QR válido: EsCodigoQrValido == true y el contexto queda poblado.
    public async Task QrValido_PoblaContexto()
    {
        var esc = new Escenario(IndividualActiva(), qrValido: true);
        var contexto = Contexto(ParticipanteId);

        await esc.Cadena().ManejarAsync(contexto, CancellationToken.None);

        contexto.EsCodigoQrValido.Should().BeTrue();
        contexto.Sesion.Should().NotBeNull();
        contexto.Participante.Should().NotBeNull();
        contexto.EquipoId.Should().BeNull(); // individual
        contexto.TotalCompetidores.Should().Be(1);
    }

    // ---------------------------------------------------------------- I, J (espías del patrón)

    // Espía que hereda de la clase base real para observar el orden de ejecución
    // y demostrar la mecánica EstablecerSiguiente/ManejarAsync/_siguiente.
    private sealed class EslabonEspia : EslabonValidacionEvidenciaTesoroBase
    {
        private readonly string _nombre;
        private readonly List<string> _registro;
        private readonly bool _lanza;

        public EslabonEspia(string nombre, List<string> registro, bool lanza = false)
        {
            _nombre = nombre;
            _registro = registro;
            _lanza = lanza;
        }

        protected override Task ProcesarAsync(
            ContextoValidacionEvidenciaTesoro contexto, CancellationToken cancelacion)
        {
            _registro.Add(_nombre);
            if (_lanza)
                throw new InvalidOperationException($"{_nombre} detiene la cadena");
            return Task.CompletedTask;
        }
    }

    [Fact] // (I) Cadena completa: los eslabones se ejecutan en el orden enlazado.
    public async Task Cadena_EjecutaEslabonesEnOrden()
    {
        var registro = new List<string>();
        var s1 = new EslabonEspia("uno", registro);
        var s2 = new EslabonEspia("dos", registro);
        var s3 = new EslabonEspia("tres", registro);
        s1.EstablecerSiguiente(s2).EstablecerSiguiente(s3);

        await s1.ManejarAsync(Contexto(ParticipanteId), CancellationToken.None);

        registro.Should().Equal("uno", "dos", "tres");
    }

    [Fact] // (J) Si un eslabón falla, el siguiente no se ejecuta.
    public async Task Cadena_SiEslabonFalla_NoEjecutaSiguiente()
    {
        var registro = new List<string>();
        var s1 = new EslabonEspia("uno", registro);
        var s2 = new EslabonEspia("dos", registro, lanza: true);
        var s3 = new EslabonEspia("tres", registro);
        s1.EstablecerSiguiente(s2).EstablecerSiguiente(s3);

        Func<Task> accion = () => s1.ManejarAsync(Contexto(ParticipanteId), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        registro.Should().Equal("uno", "dos"); // "tres" nunca se ejecuta
    }
}
