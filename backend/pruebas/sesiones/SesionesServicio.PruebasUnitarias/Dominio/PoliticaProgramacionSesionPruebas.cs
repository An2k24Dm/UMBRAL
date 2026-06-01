using System;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU34 — La política de dominio se prueba directamente con valores
// fijos. NO depende de DateTime.UtcNow ni de IProveedorFechaHora: la
// hora actual llega como parámetro, lo cual la hace 100% determinística.
public class PoliticaProgramacionSesionPruebas
{
    private static readonly DateTime AhoraUtc =
        new(2026, 5, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FechaProgramadaFutura_NoLanzaExcepcion()
    {
        Action accion = () => PoliticaProgramacionSesion.ValidarFechaProgramada(
            AhoraUtc.AddMinutes(1), AhoraUtc);
        accion.Should().NotThrow();
    }

    [Fact]
    public void FechaProgramadaIgualAhora_LanzaSesionInvalida()
    {
        Action accion = () => PoliticaProgramacionSesion.ValidarFechaProgramada(
            AhoraUtc, AhoraUtc);

        accion.Should()
            .Throw<SesionInvalidaExcepcion>()
            .WithMessage("La sesión no puede programarse para una fecha y hora que ya pasó.");
    }

    [Fact]
    public void FechaProgramadaPasada_LanzaSesionInvalida()
    {
        Action accion = () => PoliticaProgramacionSesion.ValidarFechaProgramada(
            AhoraUtc.AddMinutes(-1), AhoraUtc);

        accion.Should()
            .Throw<SesionInvalidaExcepcion>()
            .WithMessage("La sesión no puede programarse para una fecha y hora que ya pasó.");
    }

    [Fact]
    public void FechaProgramadaUnSegundoAdelante_NoLanza()
    {
        // Confirmación del límite: estrictamente > ahora.
        Action accion = () => PoliticaProgramacionSesion.ValidarFechaProgramada(
            AhoraUtc.AddSeconds(1), AhoraUtc);
        accion.Should().NotThrow();
    }
}
