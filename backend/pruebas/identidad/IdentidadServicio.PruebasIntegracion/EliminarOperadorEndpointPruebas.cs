using System.Net;
using System.Net.Http.Json;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.PruebasIntegracion;

// HU13 — pruebas de integración del endpoint
// DELETE /api/usuarios/operadores/{id}. Cubren:
//  * Autorización (401 anónimo, 403 Operador, 403 Participante).
//  * Administrador elimina un Operador y desaparece de PostgreSQL en
//    cascada (Usuario → Persona → Operador).
//  * 404 si el id es inexistente.
//  * 404 si el id corresponde a un Administrador o Participante (no se
//    puede eliminar Administradores ni Participantes por esta vía).
//  * Después de eliminar, el detalle ya no se puede consultar.
//  * Si Keycloak falla, la base no queda eliminada.
//  * La respuesta no contiene datos sensibles.
//
// Cada caso que muta la base usa un Operador propio para no romper otras
// pruebas que dependen del Operador sembrado en FabricaApiPruebas (HU09
// modificar Operador, listado HU08, etc.).
public class EliminarOperadorEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public EliminarOperadorEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Delete(Guid id, string? rol)
    {
        var solicitud = new HttpRequestMessage(
            HttpMethod.Delete, $"/api/usuarios/operadores/{id}");
        if (rol is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        return solicitud;
    }

    // Inserta un Operador aislado para que cada caso no rompa otras pruebas.
    // Devuelve el id generado. Los valores cumplen los constraints del esquema:
    // teléfono único de 11 dígitos, nombre/apellido alfabéticos, etc.
    private Guid SembrarOperadorAislado(string sufijo, int indice)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var ahora = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        var nac = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var idUsuario = Guid.NewGuid();
        // Teléfono único entre pruebas: 0414 + 7 dígitos del índice.
        var telefono = $"0414{indice:D7}";

        contexto.Usuarios.Add(new UsuarioModelo
        {
            Id = idUsuario,
            NombreUsuario = $"op_hu13_{sufijo}",
            IdKeycloak = $"kc-op-hu13-{sufijo}",
            Rol = (int)RolUsuario.Operador,
            Estado = (int)EstadoUsuario.Activo,
            FechaRegistro = ahora
        });
        var persona = new PersonaModelo
        {
            Id = Guid.NewGuid(),
            UsuarioId = idUsuario,
            Nombre = "Hache",
            Apellido = "OpHU",
            Correo = $"op_hu13_{sufijo}@umbral.com",
            Direccion = "Av. Caracas, Caracas",
            Telefono = telefono,
            Sexo = (int)SexoPersona.Femenino,
            FechaNacimiento = nac,
            FechaRegistro = ahora
        };
        contexto.Personas.Add(persona);
        contexto.Operadores.Add(new OperadorModelo
        {
            Id = Guid.NewGuid(),
            PersonaId = persona.Id,
            CodigoOperador = $"OP-HU13-{sufijo}",
            FechaRegistro = ahora
        });
        contexto.SaveChanges();
        return idUsuario;
    }

    private (int usuarios, int personas, int operadores) ContarPorId(Guid idUsuario)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var usuario = contexto.Usuarios.AsNoTracking()
            .FirstOrDefault(u => u.Id == idUsuario);
        if (usuario is null) return (0, 0, 0);
        var personas = contexto.Personas.AsNoTracking()
            .Count(p => p.UsuarioId == usuario.Id);
        var personaIds = contexto.Personas.AsNoTracking()
            .Where(p => p.UsuarioId == usuario.Id).Select(p => p.Id).ToList();
        var operadores = contexto.Operadores.AsNoTracking()
            .Count(o => personaIds.Contains(o.PersonaId));
        return (1, personas, operadores);
    }

    [Fact]
    public async Task Delete_SinToken_Retorna401()
    {
        var respuesta = await _cliente.SendAsync(Delete(Guid.NewGuid(), rol: null));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_TokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Delete(FabricaApiPruebas.IdOperadorSembrado, "Operador"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_TokenParticipante_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Delete(FabricaApiPruebas.IdOperadorSembrado, "Participante"));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_TokenAdministrador_EliminaOperador()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var id = SembrarOperadorAislado("ok", indice: 21);

        var respuesta = await _cliente.SendAsync(Delete(id, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<EliminarOperadorRespuestaDto>();
        dto!.Eliminado.Should().BeTrue();
        dto.IdOperador.Should().Be(id);
        dto.Mensaje.Should().NotBeNullOrWhiteSpace();

        // Cascada Usuario → Persona → Operador.
        var conteo = ContarPorId(id);
        conteo.usuarios.Should().Be(0);
        conteo.personas.Should().Be(0);
        conteo.operadores.Should().Be(0);

        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            $"kc-op-hu13-ok", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_IdInexistente_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(
            Delete(Guid.NewGuid(), "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("OPERADOR_NO_ENCONTRADO");
    }

    [Fact]
    public async Task Delete_IdDeAdministrador_Retorna404_YNoElimina()
    {
        // No se puede eliminar Administradores por este endpoint: el
        // repositorio filtra estrictamente por rol Operador, así que
        // ObtenerPorIdAsync devuelve null y el manejador responde 404.
        _fabrica.MockProveedor.Invocations.Clear();

        Guid idAdmin;
        using (var alcance = _fabrica.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
            idAdmin = contexto.Usuarios.AsNoTracking()
                .First(u => u.Rol == (int)RolUsuario.Administrador).Id;
        }

        var respuesta = await _cliente.SendAsync(Delete(idAdmin, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        // El Administrador sigue existiendo en BD.
        var conteo = ContarPorId(idAdmin);
        conteo.usuarios.Should().Be(1);
    }

    [Fact]
    public async Task Delete_IdDeParticipante_Retorna404_YNoElimina()
    {
        // Mismo blindaje que con Administrador.
        _fabrica.MockProveedor.Invocations.Clear();
        Guid idParticipante;
        using (var alcance = _fabrica.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
            idParticipante = contexto.Usuarios.AsNoTracking()
                .First(u => u.Rol == (int)RolUsuario.Participante &&
                            u.Estado == (int)EstadoUsuario.Activo).Id;
        }

        var respuesta = await _cliente.SendAsync(Delete(idParticipante, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        ContarPorId(idParticipante).usuarios.Should().Be(1);
    }

    [Fact]
    public async Task Delete_DespuesDeEliminar_ElDetalleYaNoSeConsulta()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var id = SembrarOperadorAislado("detalle", indice: 22);

        var elim = await _cliente.SendAsync(Delete(id, "Administrador"));
        elim.StatusCode.Should().Be(HttpStatusCode.OK);

        // Consulta de detalle: con el mismo Administrador, el endpoint
        // /api/usuarios/internos/{id} debe responder 404.
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Get, $"/api/usuarios/internos/{id}");
        solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        var detalle = await _cliente.SendAsync(solicitud);
        detalle.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_KeycloakFalla_NoBorraBaseDatos()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak caído"));

        var id = SembrarOperadorAislado("fallakc", indice: 23);

        var respuesta = await _cliente.SendAsync(Delete(id, "Administrador"));

        // Excepción no controlada → 500 con cuerpo genérico. No filtra el
        // detalle interno de Keycloak.
        ((int)respuesta.StatusCode).Should().Be((int)HttpStatusCode.InternalServerError);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().NotContain("Keycloak caído");

        // Base intacta: la transacción NO se confirmó.
        var conteo = ContarPorId(id);
        conteo.usuarios.Should().Be(1);
        conteo.personas.Should().Be(1);
        conteo.operadores.Should().Be(1);

        // Restauramos el mock para el resto de pruebas.
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Delete_Respuesta_NoExponeDatosSensibles()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var id = SembrarOperadorAislado("nosens", indice: 24);

        var respuesta = await _cliente.SendAsync(Delete(id, "Administrador"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().NotContain("op_hu13_nosens");
        json.Should().NotContain("op_hu13_nosens@umbral.com");
        json.Should().NotContain("kc-op-hu13-nosens");
        json.Should().NotContain("OP-HU13-nosens");
    }
}
