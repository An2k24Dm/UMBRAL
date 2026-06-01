using System.Reflection;
using IdentidadServicio.Api.Controladores;
using Microsoft.AspNetCore.Authorization;

namespace IdentidadServicio.PruebasUnitarias.Api;

public class UsuariosControladorAutorizacionPruebas
{
    private const string PoliticaEsperada = "PoliticaAdministradorUOperador";

    private static MethodInfo MetodoPublico(string nombre) =>
        typeof(UsuariosControlador).GetMethod(nombre,
            BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException(
            $"No se encontró el método '{nombre}' en UsuariosControlador.");

    [Theory]
    [InlineData(nameof(UsuariosControlador.ConsultarParticipantes))]
    [InlineData(nameof(UsuariosControlador.ObtenerDetalleParticipante))]
    public void Endpoints_HU07_TienenPoliticaAdministradorUOperador(string nombreMetodo)
    {
        var metodo = MetodoPublico(nombreMetodo);
        var autorizacion = metodo.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .ToArray();

        autorizacion.Should().NotBeEmpty(
            "los endpoints de HU07 deben estar protegidos con [Authorize].");
        autorizacion.Should().Contain(a => a.Policy == PoliticaEsperada,
            $"el endpoint '{nombreMetodo}' debe exigir la política '{PoliticaEsperada}'.");
    }
}
