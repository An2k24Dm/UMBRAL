using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

// Verifica que las capacidades configurables viajan dominio → modelo de
// persistencia → dominio sin perderse (cubre el guardado y la reconstrucción
// al consultar detalle, sin depender de una base de datos real).
public class MapeadorSesionesPersistenciaPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly MapeadorSesionesPersistencia Mapeador =
        new(new IMapeadorPersistenciaSesion[]
        {
            new MapeadorPersistenciaSesionIndividual(),
            new MapeadorPersistenciaSesionGrupal()
        });

    [Fact]
    public void Individual_GuardaYReconstruyeCapacidad()
    {
        var sesion = SesionIndividual.Crear(
            "Piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoParticipantes: 17);

        var modelo = Mapeador.HaciaModelo(sesion);
        modelo.MaximoParticipantes.Should().Be(17);
        modelo.MaximoEquipos.Should().BeNull();
        modelo.MaximoParticipantesPorEquipo.Should().BeNull();

        var reconstruida = (SesionIndividual)Mapeador.HaciaDominio(modelo);
        reconstruida.MaximoParticipantes.Should().Be(17);
    }

    [Fact]
    public void Grupal_GuardaYReconstruyeCapacidades()
    {
        var sesion = SesionGrupal.Crear(
            "Piloto", "Demo", AhoraUtc.AddHours(1), "DEF456", Operador, AhoraUtc,
            maximoEquipos: 7, maximoParticipantesPorEquipo: 4);

        var modelo = Mapeador.HaciaModelo(sesion);
        modelo.MaximoEquipos.Should().Be(7);
        modelo.MaximoParticipantesPorEquipo.Should().Be(4);
        modelo.MaximoParticipantes.Should().BeNull();

        var reconstruida = (SesionGrupal)Mapeador.HaciaDominio(modelo);
        reconstruida.MaximoEquipos.Should().Be(7);
        reconstruida.MaximoParticipantesPorEquipo.Should().Be(4);
    }

    [Fact]
    public void Individual_ModeloSinCapacidad_UsaRespaldoHistorico()
    {
        // Simula datos previos a la capacidad configurable (columna NULL).
        var sesion = SesionIndividual.Crear(
            "Piloto", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoParticipantes: 10);
        var modelo = Mapeador.HaciaModelo(sesion);
        modelo.MaximoParticipantes = null;

        var reconstruida = (SesionIndividual)Mapeador.HaciaDominio(modelo);
        reconstruida.MaximoParticipantes.Should().Be(10);
    }
}
