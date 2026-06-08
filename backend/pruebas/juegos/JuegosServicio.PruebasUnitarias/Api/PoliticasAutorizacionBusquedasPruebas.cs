using System;
using System.Linq;
using System.Reflection;
using JuegosServicio.Presentacion.Controladores;
using Microsoft.AspNetCore.Authorization;

namespace JuegosServicio.PruebasUnitarias.Api;

public class PoliticasAutorizacionBusquedasPruebas
{
    private static string PoliticaDe(Type controlador, string nombreMetodo)
    {
        var metodo = controlador.GetMethod(nombreMetodo)
            ?? throw new InvalidOperationException(
                $"No se encontró el método {nombreMetodo} en {controlador.Name}.");
        var atributo = metodo.GetCustomAttribute<AuthorizeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"El método {nombreMetodo} no tiene [Authorize].");
        return atributo.Policy ?? string.Empty;
    }

    [Fact]
    public void BusquedasControlador_ANivelDeClase_NoDebeRestringirAAdministrador()
    {
        var atributos = typeof(BusquedasControlador)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        atributos.Should().HaveCount(1);
        atributos[0].Policy.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(nameof(BusquedasControlador.ObtenerBusquedasActivas))]
    [InlineData(nameof(BusquedasControlador.ObtenerDetalleBusqueda))]
    public void BusquedasConsultasUsanPoliticaOperador(string metodo) =>
        PoliticaDe(typeof(BusquedasControlador), metodo).Should().Be("PoliticaOperador");

    [Theory]
    [InlineData(nameof(BusquedasControlador.CrearBusqueda))]
    [InlineData(nameof(BusquedasControlador.ObtenerBusquedasEnBorrador))]
    [InlineData(nameof(BusquedasControlador.ModificarBusqueda))]
    [InlineData(nameof(BusquedasControlador.ActivarBusqueda))]
    [InlineData(nameof(BusquedasControlador.DesactivarBusqueda))]
    [InlineData(nameof(BusquedasControlador.EliminarBusqueda))]
    [InlineData(nameof(BusquedasControlador.AgregarPista))]
    [InlineData(nameof(BusquedasControlador.ModificarPista))]
    [InlineData(nameof(BusquedasControlador.EliminarPista))]
    public void BusquedasGestionUsaPoliticaAdministrador(string metodo) =>
        PoliticaDe(typeof(BusquedasControlador), metodo).Should().Be("PoliticaAdministrador");

    [Fact]
    public void MisionesControlador_ANivelDeClase_NoDebeRestringirAAdministrador()
    {
        var atributos = typeof(MisionesControlador)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList();
        atributos.Should().HaveCount(1);
        atributos[0].Policy.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(nameof(MisionesControlador.ObtenerMisionesActivas))]
    [InlineData(nameof(MisionesControlador.ObtenerDetalleMision))]
    public void MisionesConsultasUsanPoliticaOperador(string metodo) =>
        PoliticaDe(typeof(MisionesControlador), metodo).Should().Be("PoliticaOperador");

    [Theory]
    [InlineData(nameof(MisionesControlador.CrearMision))]
    [InlineData(nameof(MisionesControlador.ObtenerMisionesEnBorrador))]
    [InlineData(nameof(MisionesControlador.ModificarMision))]
    [InlineData(nameof(MisionesControlador.ActivarMision))]
    [InlineData(nameof(MisionesControlador.DesactivarMision))]
    [InlineData(nameof(MisionesControlador.EliminarMision))]
    [InlineData(nameof(MisionesControlador.AgregarEtapa))]
    [InlineData(nameof(MisionesControlador.EliminarEtapa))]
    public void MisionesGestionUsaPoliticaAdministrador(string metodo) =>
        PoliticaDe(typeof(MisionesControlador), metodo).Should().Be("PoliticaAdministrador");
}
