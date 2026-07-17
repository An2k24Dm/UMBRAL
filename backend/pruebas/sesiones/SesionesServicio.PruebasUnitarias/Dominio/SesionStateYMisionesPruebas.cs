using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Cubre el patrón State y la asignación de misiones a nivel del padre
// abstracto Sesion. Ambas hijas deben heredar el mismo comportamiento.
public class SesionStateYMisionesPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionIndividual NuevaIndividual()
        => SesionIndividual.Crear(
            "I", "Demo", AhoraUtc.AddHours(1), "I-ABC", Operador, AhoraUtc,
            maximoParticipantes: 10);

    private static SesionGrupal NuevaGrupal()
        => SesionGrupal.Crear(
            "G", "Demo", AhoraUtc.AddHours(1), "G-DEF", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);

    [Fact]
    public void Individual_TransitaProgramadaAEnPreparacionActivaPausadaActivaFinalizada()
    {
        var s = NuevaIndividual();
        s.Estado.Should().Be(EstadoSesion.Programada);
        s.Preparar();
        s.Estado.Should().Be(EstadoSesion.EnPreparacion);
        s.Iniciar(AhoraUtc);
        s.Estado.Should().Be(EstadoSesion.Activa);
        s.FechaInicioUtc.Should().Be(AhoraUtc);
        s.Pausar();
        s.Estado.Should().Be(EstadoSesion.Pausada);
        s.Reanudar();
        s.Estado.Should().Be(EstadoSesion.Activa);
        s.Finalizar(AhoraUtc.AddMinutes(30));
        s.Estado.Should().Be(EstadoSesion.Finalizada);
        s.FechaFinalizacionUtc.Should().Be(AhoraUtc.AddMinutes(30));
    }

    [Fact]
    public void Grupal_TransitaIgualQueIndividual()
    {
        var s = NuevaGrupal();
        s.Preparar();
        s.Iniciar(AhoraUtc);
        s.Pausar();
        s.Reanudar();
        s.Finalizar(AhoraUtc.AddMinutes(30));
        s.Estado.Should().Be(EstadoSesion.Finalizada);
    }

    [Fact]
    public void TransicionInvalida_DesdeProgramada_Iniciar_Lanza()
    {
        var s = NuevaIndividual();
        Action accion = () => s.Iniciar(AhoraUtc);
        accion.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    [Fact]
    public void Programada_Preparar_PasaAEnPreparacion()
    {
        var s = NuevaGrupal();

        s.Preparar();

        s.Estado.Should().Be(EstadoSesion.EnPreparacion);
    }

    [Fact]
    public void Programada_Cancelar_LanzaYConservaEstado()
    {
        var s = NuevaGrupal();

        Action accion = () => s.Cancelar();

        accion.Should()
            .Throw<TransicionEstadoSesionInvalidaExcepcion>()
            .WithMessage("Una sesión programada no se cancela; debe eliminarse.");
        s.Estado.Should().Be(EstadoSesion.Programada);
    }

    [Fact]
    public void EnPreparacion_Cancelar_PasaACancelada()
    {
        var s = NuevaIndividual();
        s.Preparar();

        s.Cancelar();

        s.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Activa_Cancelar_PasaACancelada()
    {
        var s = NuevaIndividual();
        s.Preparar();
        s.Iniciar(AhoraUtc);

        s.Cancelar();

        s.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Pausada_Cancelar_PasaACancelada()
    {
        var s = NuevaIndividual();
        s.Preparar();
        s.Iniciar(AhoraUtc);
        s.Pausar();

        s.Cancelar();

        s.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Finalizada_Cancelar_LanzaYConservaEstado()
    {
        var s = NuevaIndividual();
        s.Preparar();
        s.Iniciar(AhoraUtc);
        s.Finalizar(AhoraUtc.AddMinutes(30));

        Action accion = () => s.Cancelar();

        accion.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        s.Estado.Should().Be(EstadoSesion.Finalizada);
    }

    [Fact]
    public void Cancelada_NoPermiteNuevasTransiciones()
    {
        var s = NuevaIndividual();
        s.Preparar();
        s.Cancelar();

        Action preparar = () => s.Preparar();
        Action iniciar = () => s.Iniciar(AhoraUtc);
        Action pausar = () => s.Pausar();
        Action reanudar = () => s.Reanudar();
        Action finalizar = () => s.Finalizar(AhoraUtc);
        Action cancelar = () => s.Cancelar();

        preparar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        iniciar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        pausar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        reanudar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        finalizar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        cancelar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        s.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Programada_ValidarPuedeEliminarse_NoLanza()
    {
        var s = NuevaIndividual();

        Action accion = () => s.ValidarPuedeEliminarse();

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public void NoProgramada_ValidarPuedeEliminarse_Lanza(EstadoSesion estado)
    {
        var s = SesionIndividual.Rehidratar(
            Guid.NewGuid(), "I", "Demo", estado,
            AhoraUtc.AddHours(1), "I-ABC", Operador, AhoraUtc,
            null, null, 10);

        Action accion = () => s.ValidarPuedeEliminarse();

        accion.Should()
            .Throw<SesionNoEliminableExcepcion>()
            .WithMessage("Solo se pueden eliminar sesiones en estado Programada.");
    }

    [Fact]
    public void AsignarMisiones_SinMisiones_Lanza()
    {
        var s = NuevaIndividual();
        Action accion = () => s.AsignarMisiones(Array.Empty<Guid>());
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_MasDelMaximo_Lanza()
    {
        var s = NuevaGrupal();
        var lista = Enumerable.Range(0,
            PoliticaCapacidadSesion.MaximoMisionesPorSesion + 1)
            .Select(_ => Guid.NewGuid()).ToList();
        Action accion = () => s.AsignarMisiones(lista);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_Repetidas_Lanza()
    {
        var s = NuevaIndividual();
        var repetida = Guid.NewGuid();
        Action accion = () => s.AsignarMisiones(new[] { repetida, repetida });
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void AsignarMisiones_GuidEmpty_Lanza()
    {
        var s = NuevaIndividual();
        Action accion = () => s.AsignarMisiones(new[] { Guid.Empty });
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void ModificarDatosBasicos_DescripcionVacia_Lanza()
    {
        var s = NuevaIndividual();

        Action accion = () => s.ModificarDatosBasicos(
            "Nueva", "   ", AhoraUtc.AddHours(2), AhoraUtc);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*descripción*");
    }

    [Fact]
    public void EstablecerSecuenciaEtapas_RechazaNullVacioNoPlanificadaOrdenYEtapaDuplicados()
    {
        var s = NuevaIndividual();
        var mision = Guid.NewGuid();
        var etapa = Guid.NewGuid();
        var modo = Guid.NewGuid();
        var planificada = EjecucionActualSesion.Planificar(
            mision, etapa, modo, "Trivia", 1, 1, 1, 60);
        var activa = EjecucionActualSesion.Crear(
            mision, Guid.NewGuid(), modo, "Trivia", 2, AhoraUtc, 60);

        s.Invoking(x => x.EstablecerSecuenciaEtapas(null!))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*secuencia*");
        s.Invoking(x => x.EstablecerSecuenciaEtapas(Array.Empty<EjecucionActualSesion>()))
            .Should().Throw<MisionSinEtapasExcepcion>();
        s.Invoking(x => x.EstablecerSecuenciaEtapas(new[] { activa }))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*Planificada*");
        s.Invoking(x => x.EstablecerSecuenciaEtapas(new[]
            {
                planificada,
                EjecucionActualSesion.Planificar(
                    mision, Guid.NewGuid(), modo, "Trivia", 1, 1, 2, 60)
            }))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*orden global*");
        s.Invoking(x => x.EstablecerSecuenciaEtapas(new[]
            {
                planificada,
                EjecucionActualSesion.Planificar(
                    mision, etapa, modo, "Trivia", 2, 1, 2, 60)
            }))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*repetir etapas*");
    }

    [Fact]
    public void AvanzarYCompletarEtapa_ValidanEstadoExistenciaYEtapaActual()
    {
        var etapaActual = Guid.NewGuid();
        var ejecucion = EjecucionActualSesion.Crear(
            Guid.NewGuid(), etapaActual, Guid.NewGuid(), "Trivia", 1, AhoraUtc, 60);
        var activa = SesionIndividual.Rehidratar(
            Guid.NewGuid(), "S", "D", EstadoSesion.Activa,
            AhoraUtc.AddHours(1), "COD", Operador, AhoraUtc,
            AhoraUtc, null, 5, ejecucionActual: ejecucion);
        var sinEjecucion = SesionIndividual.Rehidratar(
            Guid.NewGuid(), "S", "D", EstadoSesion.Activa,
            AhoraUtc.AddHours(1), "COD2", Operador, AhoraUtc,
            AhoraUtc, null, 5);
        var programada = NuevaIndividual();

        programada.Invoking(s => s.AvanzarASiguienteEtapa(
                etapaActual, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Trivia", 2, AhoraUtc, 60))
            .Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
        sinEjecucion.Invoking(s => s.AvanzarASiguienteEtapa(
                etapaActual, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Trivia", 2, AhoraUtc, 60))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*etapa global activa*");
        activa.Invoking(s => s.AvanzarASiguienteEtapa(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Trivia", 2, AhoraUtc, 60))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*etapa indicada*");
        sinEjecucion.Invoking(s => s.CompletarUltimaEtapa(etapaActual))
            .Should().Throw<SesionInvalidaExcepcion>();
        activa.Invoking(s => s.CompletarUltimaEtapa(Guid.NewGuid()))
            .Should().Throw<SesionInvalidaExcepcion>();

        activa.CompletarUltimaEtapa(etapaActual);

        activa.EjecucionActual.Should().BeNull();
    }

    [Fact]
    public void ProgramarActivarYCerrarEtapa_ValidanPrecondicionesYActualizanEjecucion()
    {
        var etapaActual = Guid.NewGuid();
        var siguienteEtapa = Guid.NewGuid();
        var ejecucion = EjecucionActualSesion.Crear(
            Guid.NewGuid(), etapaActual, Guid.NewGuid(), "Trivia", 1, AhoraUtc, 60);
        var activa = SesionIndividual.Rehidratar(
            Guid.NewGuid(), "S", "D", EstadoSesion.Activa,
            AhoraUtc.AddHours(1), "COD", Operador, AhoraUtc,
            AhoraUtc, null, 5, ejecucionActual: ejecucion);
        var siguiente = EjecucionActualSesion.Planificar(
            Guid.NewGuid(), siguienteEtapa, Guid.NewGuid(), "BusquedaTesoro", 2, 1, 2, 90);

        activa.Invoking(s => s.ProgramarSiguienteEtapa(
                Guid.NewGuid(), siguiente, AhoraUtc.AddMinutes(1), 10))
            .Should().Throw<SesionInvalidaExcepcion>();
        activa.Invoking(s => s.ProgramarSiguienteEtapa(
                etapaActual, null!, AhoraUtc.AddMinutes(1), 10))
            .Should().Throw<SesionInvalidaExcepcion>();
        activa.Invoking(s => s.ProgramarSiguienteEtapa(
                etapaActual,
                EjecucionActualSesion.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    "Trivia", 3, AhoraUtc, 60),
                AhoraUtc.AddMinutes(1),
                10))
            .Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*Planificada*");

        activa.ProgramarSiguienteEtapa(etapaActual, siguiente, AhoraUtc.AddMinutes(1), 10);
        activa.EjecucionActual!.EtapaId.Should().Be(siguienteEtapa);
        activa.EjecucionActual.EstaEnPreparacion.Should().BeTrue();

        activa.ActivarEtapaProgramada(siguienteEtapa, AhoraUtc.AddMinutes(2));
        activa.EjecucionActual!.EstaActiva.Should().BeTrue();
        activa.ProgramarCierrePendiente(siguienteEtapa, AhoraUtc.AddMinutes(3), 15);
        activa.EjecucionActual!.EstaEnCierrePendiente.Should().BeTrue();
    }
}
