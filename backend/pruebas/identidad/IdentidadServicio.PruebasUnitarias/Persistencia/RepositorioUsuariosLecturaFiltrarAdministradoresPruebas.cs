using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia;
using IdentidadServicio.Infraestructura.Persistencia.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.PruebasUnitarias.Persistencia;

// HU34 — El filtrado de administradores se hace por IdKeycloak (el sub
// que sesiones-servicio guarda en CreadaPorUsuarioId), NO por Usuario.Id
// (UUID interno UMBRAL). Esta suite sembra ambos tipos de id distintos
// para evitar regresiones donde alguien vuelva a filtrar por Id interno.
public class RepositorioUsuariosLecturaFiltrarAdministradoresPruebas
{
    private static ContextoIdentidad CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoIdentidad>()
            .UseInMemoryDatabase($"identidad-{Guid.NewGuid()}")
            .Options;
        return new ContextoIdentidad(opciones);
    }

    private static async Task SembrarUsuarioAsync(
        ContextoIdentidad contexto, Guid idInterno, Guid idKeycloak, RolUsuario rol)
    {
        contexto.Usuarios.Add(new UsuarioModelo
        {
            Id = idInterno,
            IdKeycloak = idKeycloak.ToString(),
            NombreUsuario = $"u-{idInterno.ToString()[..4]}",
            Rol = (int)rol,
            Estado = 0,
            FechaRegistro = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task ConIdsKeycloakDeAdministradores_DevuelveEsosMismosIds()
    {
        await using var ctx = CrearContexto();
        var keycloakAdmin = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var keycloakOperador = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // IDs internos UMBRAL distintos al sub de Keycloak: así
        // confirmamos que la consulta usa IdKeycloak, no Id.
        await SembrarUsuarioAsync(ctx,
            idInterno: Guid.NewGuid(), idKeycloak: keycloakAdmin, rol: RolUsuario.Administrador);
        await SembrarUsuarioAsync(ctx,
            idInterno: Guid.NewGuid(), idKeycloak: keycloakOperador, rol: RolUsuario.Operador);

        var repo = new RepositorioUsuariosLectura(ctx);
        var resultado = await repo.FiltrarAdministradoresPorIdsAsync(
            new[] { keycloakAdmin, keycloakOperador }, CancellationToken.None);

        resultado.Should().ContainSingle().Which.Should().Be(keycloakAdmin);
    }

    [Fact]
    public async Task NoConfundeIdInternoConSubKeycloak()
    {
        await using var ctx = CrearContexto();
        var idInternoAdmin = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var subKeycloakAdmin = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await SembrarUsuarioAsync(ctx,
            idInterno: idInternoAdmin, idKeycloak: subKeycloakAdmin, rol: RolUsuario.Administrador);

        var repo = new RepositorioUsuariosLectura(ctx);

        // Buscamos por el ID INTERNO; no debe encontrar nada porque el
        // filtro va contra IdKeycloak.
        var resultadoConIdInterno = await repo.FiltrarAdministradoresPorIdsAsync(
            new[] { idInternoAdmin }, CancellationToken.None);
        resultadoConIdInterno.Should().BeEmpty();

        // Buscamos por el sub real: ahora sí lo encuentra.
        var resultadoConSub = await repo.FiltrarAdministradoresPorIdsAsync(
            new[] { subKeycloakAdmin }, CancellationToken.None);
        resultadoConSub.Should().ContainSingle().Which.Should().Be(subKeycloakAdmin);
    }

    [Fact]
    public async Task ListaVacia_DevuelveVacio_SinTocarBaseDeDatos()
    {
        await using var ctx = CrearContexto();
        var repo = new RepositorioUsuariosLectura(ctx);

        var resultado = await repo.FiltrarAdministradoresPorIdsAsync(
            Array.Empty<Guid>(), CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task SoloOperadores_DevuelveVacio()
    {
        await using var ctx = CrearContexto();
        var sub = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await SembrarUsuarioAsync(ctx,
            idInterno: Guid.NewGuid(), idKeycloak: sub, rol: RolUsuario.Operador);

        var repo = new RepositorioUsuariosLectura(ctx);
        var resultado = await repo.FiltrarAdministradoresPorIdsAsync(
            new[] { sub }, CancellationToken.None);

        resultado.Should().BeEmpty();
    }
}
