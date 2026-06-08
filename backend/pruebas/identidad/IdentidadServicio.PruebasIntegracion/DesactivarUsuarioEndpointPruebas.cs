using System.Net;
using System.Net.Http.Json;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.PruebasIntegracion;

// HU12 — pruebas de integración de los endpoints PATCH:
//  * /api/usuarios/operadores/{id}/desactivar
//  * /api/usuarios/participantes/{id}/desactivar
// y del middleware BloqueoUsuarioInactivoMiddleware (bloqueo de tokens
// emitidos antes de la desactivación).
//
// Cada caso siembra su propio Operador o Participante para no chocar con
// otros tests (HU07/HU08/HU09/HU13).
public class DesactivarUsuarioEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public DesactivarUsuarioEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Patch(string url, string? rol, string? idKeycloak = null)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(new { }) // PATCH sin cuerpo significativo
        };
        if (rol is not null) solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        if (idKeycloak is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, idKeycloak);
        return solicitud;
    }

    private Guid SembrarOperador(string sufijo, int indice, EstadoUsuario estado)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var ahora = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        var nac = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var idUsuario = Guid.NewGuid();
        contexto.Usuarios.Add(new UsuarioModelo
        {
            Id = idUsuario, NombreUsuario = $"op_hu12_{sufijo}",
            IdKeycloak = $"kc-op-hu12-{sufijo}",
            Rol = (int)RolUsuario.Operador, Estado = (int)estado, FechaRegistro = ahora
        });
        var persona = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idUsuario,
            Nombre = "Hache", Apellido = "OpHU",
            Correo = $"op_hu12_{sufijo}@umbral.com",
            Direccion = "Av. Caracas, Caracas",
            Telefono = $"0414{indice:D7}",
            Sexo = (int)SexoPersona.Femenino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        contexto.Personas.Add(persona);
        contexto.Operadores.Add(new OperadorModelo
        {
            Id = Guid.NewGuid(), PersonaId = persona.Id,
            CodigoOperador = $"OP-HU12-{sufijo}", FechaRegistro = ahora
        });
        contexto.SaveChanges();
        return idUsuario;
    }

    private Guid SembrarParticipante(string sufijo, int indice, EstadoUsuario estado)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var ahora = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        var nac = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var idUsuario = Guid.NewGuid();
        contexto.Usuarios.Add(new UsuarioModelo
        {
            Id = idUsuario, NombreUsuario = $"p_hu12_{sufijo}",
            IdKeycloak = $"kc-p-hu12-{sufijo}",
            Rol = (int)RolUsuario.Participante, Estado = (int)estado, FechaRegistro = ahora
        });
        var persona = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idUsuario,
            Nombre = "Hache", Apellido = "ParHU",
            Correo = $"p_hu12_{sufijo}@umbral.com",
            Direccion = "Av. Caracas, Caracas",
            Telefono = $"0414{indice:D7}",
            Sexo = (int)SexoPersona.Masculino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        contexto.Personas.Add(persona);
        contexto.Participantes.Add(new ParticipanteModelo
        {
            Id = Guid.NewGuid(), PersonaId = persona.Id,
            Alias = $"phu12{sufijo}", FechaRegistro = ahora
        });
        contexto.SaveChanges();
        return idUsuario;
    }

    private EstadoUsuario? LeerEstado(Guid idUsuario)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var u = contexto.Usuarios.AsNoTracking().FirstOrDefault(x => x.Id == idUsuario);
        return u is null ? null : (EstadoUsuario)u.Estado;
    }

    // -----------------------------------------------------------------------
    // Desactivar Operador
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DesactivarOperador_SinToken_Retorna401()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{Guid.NewGuid()}/desactivar", rol: null));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DesactivarOperador_TokenOperador_Retorna403()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{Guid.NewGuid()}/desactivar", "Operador"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DesactivarOperador_TokenParticipante_Retorna403()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{Guid.NewGuid()}/desactivar", "Participante"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DesactivarOperador_TokenAdministrador_Cambia_A_Inactivo()
    {
        var id = SembrarOperador("admin", indice: 31, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{id}/desactivar",
                rol: "Administrador",
                idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<CambiarEstadoUsuarioRespuestaDto>();
        dto!.IdUsuario.Should().Be(id);
        dto.Estado.Should().Be("Inactivo");
        LeerEstado(id).Should().Be(EstadoUsuario.Inactivo);
    }

    [Fact]
    public async Task DesactivarOperador_IdInexistente_Retorna404()
    {
        var resp = await _cliente.SendAsync(
            Patch(
                $"/api/usuarios/operadores/{Guid.NewGuid()}/desactivar",
                rol: "Administrador",
                idKeycloak: "kc-admin"));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DesactivarOperador_YaInactivo_Retorna400()
    {
        var id = SembrarOperador("yainactivo", indice: 32, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(
            Patch(
                $"/api/usuarios/operadores/{id}/desactivar",
                rol: "Administrador",
                idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("USUARIO_YA_INACTIVO");
        LeerEstado(id).Should().Be(EstadoUsuario.Inactivo);
    }

    // -----------------------------------------------------------------------
    // Desactivar Participante
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DesactivarParticipante_SinToken_Retorna401()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/participantes/{Guid.NewGuid()}/desactivar", rol: null));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DesactivarParticipante_TokenParticipante_Retorna403()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/participantes/{Guid.NewGuid()}/desactivar", "Participante"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DesactivarParticipante_TokenAdministrador_Desactiva()
    {
        var id = SembrarParticipante("admin", indice: 41, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(
            Patch(
                $"/api/usuarios/participantes/{id}/desactivar",
                rol: "Administrador",
                idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        LeerEstado(id).Should().Be(EstadoUsuario.Inactivo);
    }

    [Fact]
    public async Task DesactivarParticipante_TokenOperadorActivo_Desactiva()
    {
        // El AuthHandler de pruebas emite por defecto sub "tester", que no
        // existe en la BD. El autorizador entonces rechaza con
        // AccesoNoPermitidoExcepcion → 403. Para que el Operador sea
        // reconocido, apuntamos al sub del Operador sembrado por la fábrica
        // (kc-op-hu09 está Activo en el seed).
        var id = SembrarParticipante("oper", indice: 42, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{id}/desactivar",
            rol: "Operador",
            idKeycloak: "kc-op-hu09"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        LeerEstado(id).Should().Be(EstadoUsuario.Inactivo);
    }

    [Fact]
    public async Task DesactivarParticipante_OperadorInactivo_Retorna403_PorMiddleware()
    {
        // Sembramos un Operador Inactivo. El middleware lo detecta antes de
        // que el endpoint corra y devuelve 403 CUENTA_DESACTIVADA.
        var idOpInactivo = SembrarOperador("inactivo", indice: 33, EstadoUsuario.Inactivo);
        _ = idOpInactivo;
        var idObjetivo = SembrarParticipante("victima", indice: 43, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{idObjetivo}/desactivar",
            rol: "Operador",
            idKeycloak: "kc-op-hu12-inactivo"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("CUENTA_DESACTIVADA");
        // Y el Participante objetivo NO fue tocado.
        LeerEstado(idObjetivo).Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public async Task DesactivarParticipante_YaInactivo_Retorna400()
    {
        var id = SembrarParticipante("yainactivo", indice: 44, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(
            Patch(
                $"/api/usuarios/participantes/{id}/desactivar",
                rol: "Administrador",
                idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("USUARIO_YA_INACTIVO");
    }

    [Fact]
    public async Task DesactivarParticipante_NoBorraDeBaseDatos()
    {
        var id = SembrarParticipante("intacto", indice: 45, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(
            Patch(
                $"/api/usuarios/participantes/{id}/desactivar",
                rol: "Administrador",
                idKeycloak: "kc-admin"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var u = contexto.Usuarios.AsNoTracking().FirstOrDefault(x => x.Id == id);
        u.Should().NotBeNull(); // sigue existiendo (HU12 NO borra)
        u!.IdKeycloak.Should().NotBeNullOrWhiteSpace();
        var persona = contexto.Personas.AsNoTracking().FirstOrDefault(p => p.UsuarioId == id);
        persona.Should().NotBeNull();
    }

    // -----------------------------------------------------------------------
    // BloqueoUsuarioInactivoMiddleware — afecta acciones protegidas con
    // token previo a la desactivación.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PerfilActual_ConTokenDeOperadorInactivo_Retorna403_CuentaDesactivada()
    {
        var _ = SembrarOperador("perfil", indice: 34, EstadoUsuario.Inactivo);

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get, "/api/autenticacion/perfil-actual");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, "kc-op-hu12-perfil");
        var resp = await _cliente.SendAsync(solicitud);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("CUENTA_DESACTIVADA");
    }

    [Fact]
    public async Task ModificarParticipante_ConTokenDeParticipanteInactivo_Retorna403()
    {
        var _ = SembrarParticipante("modificar", indice: 46, EstadoUsuario.Inactivo);

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Patch, "/api/usuarios/participantes/perfil")
        {
            Content = JsonContent.Create(new { nombre = "Otro" })
        };
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Participante");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, "kc-p-hu12-modificar");
        var resp = await _cliente.SendAsync(solicitud);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("CUENTA_DESACTIVADA");
    }

    [Fact]
    public async Task EliminarCuentaParticipante_ConTokenDeParticipanteInactivo_Retorna403()
    {
        // HU11 ya valida internamente (PuedeEliminarCuenta), pero la respuesta
        // exacta cambia con el middleware: ahora se obtiene 403
        // CUENTA_DESACTIVADA antes de entrar al manejador. Es coherente.
        var _ = SembrarParticipante("eliminar", indice: 47, EstadoUsuario.Inactivo);

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Delete, "/api/usuarios/participantes/perfil");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Participante");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, "kc-p-hu12-eliminar");
        var resp = await _cliente.SendAsync(solicitud);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("CUENTA_DESACTIVADA");
    }

    [Fact]
    public async Task ListarParticipantes_ConTokenDeOperadorInactivo_Retorna403()
    {
        var _ = SembrarOperador("listar", indice: 35, EstadoUsuario.Inactivo);

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get, "/api/usuarios/participantes");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Operador");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, "kc-op-hu12-listar");
        var resp = await _cliente.SendAsync(solicitud);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
