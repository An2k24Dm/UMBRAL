using System.Net;
using System.Net.Http.Json;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

// Pruebas de integración de los endpoints PATCH de reactivación:
//  * /api/usuarios/operadores/{id}/activar
//  * /api/usuarios/participantes/{id}/activar
// Verifican autorización, idempotencia, simetría con desactivación y que
// Keycloak NO se invoque para activar (la activación es solo cambio de
// Estado en PostgreSQL).
public class ActivarUsuarioEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public ActivarUsuarioEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Patch(string url, string? rol, string? idKeycloak = null)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(new { })
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
            Id = idUsuario, NombreUsuario = $"op_act_{sufijo}",
            IdKeycloak = $"kc-op-act-{sufijo}",
            Rol = (int)RolUsuario.Operador, Estado = (int)estado, FechaRegistro = ahora
        });
        var persona = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idUsuario,
            Nombre = "Hache", Apellido = "OpAct",
            Correo = $"op_act_{sufijo}@umbral.com",
            Direccion = "Av. Caracas, Caracas",
            Telefono = $"0414{indice:D7}",
            Sexo = (int)SexoPersona.Femenino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        contexto.Personas.Add(persona);
        contexto.Operadores.Add(new OperadorModelo
        {
            Id = Guid.NewGuid(), PersonaId = persona.Id,
            CodigoOperador = $"OP-ACT-{sufijo}", FechaRegistro = ahora
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
            Id = idUsuario, NombreUsuario = $"p_act_{sufijo}",
            IdKeycloak = $"kc-p-act-{sufijo}",
            Rol = (int)RolUsuario.Participante, Estado = (int)estado, FechaRegistro = ahora
        });
        var persona = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idUsuario,
            Nombre = "Hache", Apellido = "ParAct",
            Correo = $"p_act_{sufijo}@umbral.com",
            Direccion = "Av. Caracas, Caracas",
            Telefono = $"0414{indice:D7}",
            Sexo = (int)SexoPersona.Masculino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        contexto.Personas.Add(persona);
        contexto.Participantes.Add(new ParticipanteModelo
        {
            Id = Guid.NewGuid(), PersonaId = persona.Id,
            Alias = $"pact{sufijo}", FechaRegistro = ahora
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
    // Activar Operador
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ActivarOperador_SinToken_Retorna401()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{Guid.NewGuid()}/activar", rol: null));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivarOperador_TokenOperador_Retorna403()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{Guid.NewGuid()}/activar", "Operador"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivarOperador_TokenParticipante_Retorna403()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/operadores/{Guid.NewGuid()}/activar", "Participante"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivarOperador_TokenAdministrador_Cambia_A_Activo()
    {
        var id = SembrarOperador("ok", indice: 51, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/operadores/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<CambiarEstadoUsuarioRespuestaDto>();
        dto!.IdUsuario.Should().Be(id);
        dto.Estado.Should().Be("Activo");
        LeerEstado(id).Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public async Task ActivarOperador_IdInexistente_Retorna404()
    {
        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/operadores/{Guid.NewGuid()}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivarOperador_YaActivo_Retorna400()
    {
        var id = SembrarOperador("yaactivo", indice: 52, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/operadores/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("USUARIO_YA_ACTIVO");
    }

    [Fact]
    public async Task ActivarOperador_NoLlamaKeycloakDelete()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        var id = SembrarOperador("noKc", indice: 53, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/operadores/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirmamos que la activación NO toca Keycloak: ni eliminar, ni
        // crear, ni cambiar contraseña, ni actualizar datos.
        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fabrica.MockProveedor.Verify(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _fabrica.MockProveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _fabrica.MockProveedor.Verify(p => p.CambiarContrasenaAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Activar Participante
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ActivarParticipante_SinToken_Retorna401()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/participantes/{Guid.NewGuid()}/activar", rol: null));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivarParticipante_TokenParticipante_Retorna403()
    {
        var resp = await _cliente.SendAsync(
            Patch($"/api/usuarios/participantes/{Guid.NewGuid()}/activar", "Participante"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivarParticipante_TokenAdministrador_Activa()
    {
        var id = SembrarParticipante("admin", indice: 61, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        LeerEstado(id).Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public async Task ActivarParticipante_TokenOperadorActivo_Activa()
    {
        // El Operador sembrado por la fábrica está Activo y su sub es
        // "kc-op-hu09".
        var id = SembrarParticipante("oper", indice: 62, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{id}/activar",
            rol: "Operador",
            idKeycloak: "kc-op-hu09"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        LeerEstado(id).Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public async Task ActivarParticipante_OperadorInactivo_Retorna403_PorMiddleware()
    {
        // Sembramos un Operador Inactivo. El middleware lo bloquea antes
        // de que el endpoint corra: 403 CUENTA_DESACTIVADA.
        var _ = SembrarOperador("opInact", indice: 54, EstadoUsuario.Inactivo);
        var idObjetivo = SembrarParticipante("victima", indice: 63, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{idObjetivo}/activar",
            rol: "Operador",
            idKeycloak: "kc-op-act-opInact"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("CUENTA_DESACTIVADA");
        LeerEstado(idObjetivo).Should().Be(EstadoUsuario.Inactivo);
    }

    [Fact]
    public async Task ActivarParticipante_YaActivo_Retorna400()
    {
        var id = SembrarParticipante("yaactivo", indice: 64, EstadoUsuario.Activo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("USUARIO_YA_ACTIVO");
    }

    [Fact]
    public async Task ActivarParticipante_NoModificaDatosNiKeycloak()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        var id = SembrarParticipante("intacto", indice: 65, EstadoUsuario.Inactivo);

        var resp = await _cliente.SendAsync(Patch(
            $"/api/usuarios/participantes/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Datos personales intactos.
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var u = contexto.Usuarios.AsNoTracking().FirstOrDefault(x => x.Id == id);
        u!.IdKeycloak.Should().Be("kc-p-act-intacto");
        u.NombreUsuario.Should().Be("p_act_intacto");
        u.Rol.Should().Be((int)RolUsuario.Participante);

        // Keycloak no fue tocado para esta acción.
        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fabrica.MockProveedor.Verify(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Simetría con desactivación: el ciclo desactivar → activar deja al
    // usuario otra vez Activo, sin pérdida de datos, sin tocar Keycloak.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CicloDesactivarLuegoActivar_DejaUsuarioActivo()
    {
        var id = SembrarOperador("ciclo", indice: 55, EstadoUsuario.Activo);

        // Desactivar
        var d = await _cliente.SendAsync(Patch(
            $"/api/usuarios/operadores/{id}/desactivar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));
        d.StatusCode.Should().Be(HttpStatusCode.OK);
        LeerEstado(id).Should().Be(EstadoUsuario.Inactivo);

        // Activar
        var a = await _cliente.SendAsync(Patch(
            $"/api/usuarios/operadores/{id}/activar",
            rol: "Administrador",
            idKeycloak: "kc-admin"));
        a.StatusCode.Should().Be(HttpStatusCode.OK);
        LeerEstado(id).Should().Be(EstadoUsuario.Activo);
    }
}
