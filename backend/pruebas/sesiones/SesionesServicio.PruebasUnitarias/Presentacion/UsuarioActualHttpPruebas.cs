using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SesionesServicio.Presentacion.Configuraciones;

namespace SesionesServicio.PruebasUnitarias.Presentacion;

public class UsuarioActualHttpPruebas
{
    [Fact]
    public void SinHttpContext_NoEstaAutenticadoYDevuelveValoresVacios()
    {
        var usuario = new UsuarioActualHttp(Mock.Of<IHttpContextAccessor>());

        usuario.EstaAutenticado().Should().BeFalse();
        usuario.ObtenerId().Should().BeNull();
        usuario.ObtenerIdKeycloak().Should().BeNull();
        usuario.ObtenerNombreUsuario().Should().BeNull();
        usuario.ObtenerRoles().Should().BeEmpty();
        usuario.TieneAlgunRol("Operador").Should().BeFalse();
        usuario.TieneAlgunRol().Should().BeFalse();
        usuario.TieneAlgunRol(null!).Should().BeFalse();
    }

    [Fact]
    public void UsuarioAutenticado_LeeSubNombreYRoles()
    {
        var id = Guid.NewGuid();
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim("sub", id.ToString()),
            new Claim("preferred_username", "participante01"),
            new Claim("roles", "Participante"),
            new Claim("roles", " ")
        }, "Bearer");
        var usuario = CrearUsuario(identidad);

        usuario.EstaAutenticado().Should().BeTrue();
        usuario.ObtenerId().Should().Be(id);
        usuario.ObtenerIdKeycloak().Should().Be(id.ToString());
        usuario.ObtenerNombreUsuario().Should().Be("participante01");
        usuario.ObtenerRoles().Should().ContainSingle().Which.Should().Be("Participante");
        usuario.TieneAlgunRol("Administrador", "Participante").Should().BeTrue();
        usuario.TieneAlgunRol("Operador").Should().BeFalse();
    }

    [Fact]
    public void UsuarioAutenticado_PriorizaNameIdentifierYName()
    {
        var id = Guid.NewGuid();
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "nombre-del-token"),
            new Claim("preferred_username", "preferido")
        }, "Bearer");
        var usuario = CrearUsuario(identidad);

        usuario.ObtenerId().Should().Be(id);
        usuario.ObtenerIdKeycloak().Should().Be(id.ToString());
        usuario.ObtenerNombreUsuario().Should().Be("nombre-del-token");
    }

    [Fact]
    public void UsuarioAutenticado_SubNoGuidDevuelveIdNullPeroConservaIdKeycloak()
    {
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim("sub", "admin-keycloak")
        }, "Bearer");
        var usuario = CrearUsuario(identidad);

        usuario.ObtenerId().Should().BeNull();
        usuario.ObtenerIdKeycloak().Should().Be("admin-keycloak");
    }

    private static UsuarioActualHttp CrearUsuario(ClaimsIdentity identidad)
    {
        var contexto = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identidad)
        };
        return new UsuarioActualHttp(Mock.Of<IHttpContextAccessor>(a => a.HttpContext == contexto));
    }
}
