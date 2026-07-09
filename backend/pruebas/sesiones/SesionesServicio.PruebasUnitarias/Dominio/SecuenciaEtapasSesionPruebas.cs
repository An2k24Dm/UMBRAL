using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Plan de etapas del agregado (Sesion.SecuenciaEtapas) como lista canónica de
// EjecucionActualSesion en fase Planificada, y compatibilidad del JSON persistido.
public class SecuenciaEtapasSesionPruebas
{
    private static readonly DateTime Ahora = new(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact] // #20 — el plan es inmutable y no cambia de fase al iniciar la ejecución.
    public void EstablecerSecuenciaEtapas_MantieneElPlanPlanificado_AlIniciarPrimeraEtapa()
    {
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var e3 = Guid.NewGuid();
        var modo = Guid.NewGuid();

        var sesion = SesionIndividual.Crear(
            "S", "D", Ahora.AddHours(1), "COD001", Guid.NewGuid(), Ahora, maximoParticipantes: 5);
        sesion.AsignarMisiones(new[] { m1, m2 });
        sesion.Preparar();
        sesion.AgregarParticipante(Guid.NewGuid(), Ahora);
        sesion.EstablecerSecuenciaEtapas(new[]
        {
            EjecucionActualSesion.Planificar(m1, e1, modo, "Trivia", 1, 1, 1, 60),
            EjecucionActualSesion.Planificar(m1, e2, modo, "Trivia", 2, 1, 2, 60),
            EjecucionActualSesion.Planificar(m2, e3, modo, "BusquedaTesoro", 3, 2, 1, 60)
        });

        sesion.SecuenciaEtapas.Should().HaveCount(3);
        sesion.SecuenciaEtapas.Select(x => x.OrdenGlobal).Should().Equal(1, 2, 3);
        sesion.SecuenciaEtapas.Should().OnlyContain(x => x.EstaPlanificada);

        sesion.IniciarPrimeraEtapa(sesion.SecuenciaEtapas[0], Ahora);

        // EjecucionActual es un snapshot runtime derivado (Activa)...
        sesion.EjecucionActual!.EtapaId.Should().Be(e1);
        sesion.EjecucionActual.EstaActiva.Should().BeTrue();
        // ...pero el PLAN sigue intacto: todas las etapas siguen Planificadas.
        sesion.SecuenciaEtapas[0].EstaPlanificada.Should().BeTrue();
        sesion.SecuenciaEtapas.Should().OnlyContain(x => x.EstaPlanificada);
    }

    [Fact] // #22 — el JSON antiguo (creado desde EtapaPlanificadaSesion) sigue siendo legible.
    public void MapearSecuenciaEtapas_LeeJsonAntiguo_ComoEtapasPlanificadas()
    {
        var mision = Guid.NewGuid();
        var etapa = Guid.NewGuid();
        var modo = Guid.NewGuid();

        var jsonAntiguo =
            "[{\"MisionId\":\"" + mision + "\",\"EtapaId\":\"" + etapa +
            "\",\"TipoEtapa\":\"Trivia\",\"ModoDeJuegoId\":\"" + modo +
            "\",\"OrdenMision\":1,\"OrdenEtapa\":1,\"OrdenGlobal\":1,\"DuracionSegundos\":60}]";

        var modelo = new SesionModelo { SecuenciaEtapasJson = jsonAntiguo };

        var secuencia = MapeadorSesionesPersistencia.MapearSecuenciaEtapas(modelo);

        secuencia.Should().HaveCount(1);
        var e = secuencia[0];
        e.EstaPlanificada.Should().BeTrue();
        e.Fase.Should().Be(FaseEjecucionEtapaSesion.Planificada);
        e.FechaInicioUtc.Should().BeNull();
        e.MisionId.Should().Be(mision);
        e.EtapaId.Should().Be(etapa);
        e.ModoDeJuegoId.Should().Be(modo);
        e.TipoEtapa.Should().Be("Trivia");
        e.OrdenMision.Should().Be(1);
        e.OrdenEtapa.Should().Be(1);
        e.OrdenGlobal.Should().Be(1);
        e.DuracionSegundos.Should().Be(60);
    }

    [Fact] // round-trip: serializar el plan y volver a leerlo produce las mismas etapas.
    public void SecuenciaEtapas_RoundTripJson_ConservaLosOchoCampos()
    {
        var mision = Guid.NewGuid();
        var etapa = Guid.NewGuid();
        var modo = Guid.NewGuid();

        var sesion = SesionIndividual.Crear(
            "S", "D", Ahora.AddHours(1), "COD002", Guid.NewGuid(), Ahora, maximoParticipantes: 5);
        sesion.AsignarMisiones(new[] { mision });
        sesion.Preparar();
        sesion.EstablecerSecuenciaEtapas(new[]
        {
            EjecucionActualSesion.Planificar(mision, etapa, modo, "BusquedaTesoro", 1, 1, 1, 90)
        });

        var mapeador = new MapeadorSesionesPersistencia(new IMapeadorPersistenciaSesion[]
        {
            new MapeadorPersistenciaSesionIndividual(),
            new MapeadorPersistenciaSesionGrupal()
        });

        var modelo = mapeador.HaciaModelo(sesion);
        var recuperada = mapeador.HaciaDominio(modelo);

        recuperada.SecuenciaEtapas.Should().HaveCount(1);
        var e = recuperada.SecuenciaEtapas[0];
        e.EstaPlanificada.Should().BeTrue();
        e.MisionId.Should().Be(mision);
        e.EtapaId.Should().Be(etapa);
        e.ModoDeJuegoId.Should().Be(modo);
        e.TipoEtapa.Should().Be("BusquedaTesoro");
        e.OrdenGlobal.Should().Be(1);
        e.DuracionSegundos.Should().Be(90);
    }
}
