using System;
using System.Linq;
using System.Reflection;
using JuegosServicio.Api.Controladores;
using Microsoft.AspNetCore.Authorization;

namespace JuegosServicio.PruebasUnitarias.Api;

// HU34 — Verifica reflexivamente las políticas de autorización del
// TriviasControlador para evitar regresiones del tipo "alguien volvió a
// poner [Authorize(Policy = "PoliticaAdministrador")] a nivel de clase
// y los endpoints de consulta dejaron de aceptar al Operador".
//
// La regla es:
//   * Gestión del catálogo  → PoliticaAdministrador.
//   * Consulta (activas, detalle) → PoliticaOperador (acepta
//     Administrador y Operador en juegos-servicio).
public class PoliticasAutorizacionTriviasPruebas
{
    private static AuthorizeAttribute? ObtenerAuthorize(MethodInfo metodo) =>
        metodo.GetCustomAttribute<AuthorizeAttribute>(inherit: false);

    private static string PoliticaDe(string nombreMetodo)
    {
        var metodo = typeof(TriviasControlador).GetMethod(nombreMetodo)
            ?? throw new InvalidOperationException(
                $"No se encontró el método {nombreMetodo} en TriviasControlador.");
        var atributo = ObtenerAuthorize(metodo)
            ?? throw new InvalidOperationException(
                $"El método {nombreMetodo} no tiene [Authorize].");
        return atributo.Policy ?? string.Empty;
    }

    [Fact]
    public void TriviasControlador_ANivelDeClase_NoDebeRestringirAAdministrador()
    {
        // Si la clase queda con [Authorize(Policy = "PoliticaAdministrador")],
        // los GET de consulta dejan de aceptar al Operador aunque cada
        // método declare otra política, porque ambas se evalúan en AND.
        var atributos = typeof(TriviasControlador)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .ToList();

        atributos.Should().HaveCount(1);
        atributos[0].Policy.Should().BeNullOrEmpty(
            "el [Authorize] a nivel de clase sólo debe exigir autenticación; " +
            "las políticas por rol viven en cada método");
    }

    [Theory]
    [InlineData(nameof(TriviasControlador.ObtenerTriviasActivas))]
    [InlineData(nameof(TriviasControlador.ObtenerDetalle))]
    public void ConsultasParaSesionesUsanPoliticaOperador(string metodo)
    {
        PoliticaDe(metodo).Should().Be("PoliticaOperador");
    }

    [Theory]
    [InlineData(nameof(TriviasControlador.CrearTrivia))]
    [InlineData(nameof(TriviasControlador.ObtenerTriviasEnBorrador))]
    [InlineData(nameof(TriviasControlador.ActivarTrivia))]
    [InlineData(nameof(TriviasControlador.ModificarTrivia))]
    [InlineData(nameof(TriviasControlador.DesactivarTrivia))]
    [InlineData(nameof(TriviasControlador.AgregarPregunta))]
    [InlineData(nameof(TriviasControlador.ModificarPregunta))]
    [InlineData(nameof(TriviasControlador.EliminarPregunta))]
    public void GestionDelCatalogoUsaPoliticaAdministrador(string metodo)
    {
        PoliticaDe(metodo).Should().Be("PoliticaAdministrador");
    }
}
