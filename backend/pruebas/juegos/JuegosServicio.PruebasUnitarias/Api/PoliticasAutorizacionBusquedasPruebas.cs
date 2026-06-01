using System;
using System.Linq;
using System.Reflection;
using JuegosServicio.Api.Controladores;
using Microsoft.AspNetCore.Authorization;

namespace JuegosServicio.PruebasUnitarias.Api;

// HU34 — Mismo control reflexivo que para Trivias, aplicado al
// BusquedasControlador. Cubre cada endpoint de gestión y los dos de
// consulta que el Operador necesita para crear y ver sesiones de
// Búsqueda del Tesoro.
public class PoliticasAutorizacionBusquedasPruebas
{
    private static AuthorizeAttribute? ObtenerAuthorize(MethodInfo metodo) =>
        metodo.GetCustomAttribute<AuthorizeAttribute>(inherit: false);

    private static string PoliticaDe(string nombreMetodo)
    {
        var metodo = typeof(BusquedasControlador).GetMethod(nombreMetodo)
            ?? throw new InvalidOperationException(
                $"No se encontró el método {nombreMetodo} en BusquedasControlador.");
        var atributo = ObtenerAuthorize(metodo)
            ?? throw new InvalidOperationException(
                $"El método {nombreMetodo} no tiene [Authorize].");
        return atributo.Policy ?? string.Empty;
    }

    [Fact]
    public void BusquedasControlador_ANivelDeClase_NoDebeRestringirAAdministrador()
    {
        var atributos = typeof(BusquedasControlador)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .ToList();

        atributos.Should().HaveCount(1);
        atributos[0].Policy.Should().BeNullOrEmpty(
            "el [Authorize] a nivel de clase sólo debe exigir autenticación; " +
            "las políticas por rol viven en cada método");
    }

    [Theory]
    [InlineData(nameof(BusquedasControlador.ObtenerBusquedasActivas))]
    [InlineData(nameof(BusquedasControlador.ObtenerDetalleBusqueda))]
    public void ConsultasParaSesionesUsanPoliticaOperador(string metodo)
    {
        PoliticaDe(metodo).Should().Be("PoliticaOperador");
    }

    [Theory]
    [InlineData(nameof(BusquedasControlador.CrearBusqueda))]
    [InlineData(nameof(BusquedasControlador.ObtenerBusquedasEnBorrador))]
    [InlineData(nameof(BusquedasControlador.ActivarBusqueda))]
    [InlineData(nameof(BusquedasControlador.ArchivarBusqueda))]
    [InlineData(nameof(BusquedasControlador.AgregarEtapa))]
    [InlineData(nameof(BusquedasControlador.ModificarEtapa))]
    [InlineData(nameof(BusquedasControlador.EliminarEtapa))]
    [InlineData(nameof(BusquedasControlador.AgregarMision))]
    [InlineData(nameof(BusquedasControlador.ModificarMision))]
    [InlineData(nameof(BusquedasControlador.EliminarMision))]
    [InlineData(nameof(BusquedasControlador.AgregarPista))]
    [InlineData(nameof(BusquedasControlador.ModificarPista))]
    [InlineData(nameof(BusquedasControlador.EliminarPista))]
    public void GestionDelCatalogoUsaPoliticaAdministrador(string metodo)
    {
        PoliticaDe(metodo).Should().Be("PoliticaAdministrador");
    }
}
