using System.Net;
using System.Net.Http.Json;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.PruebasIntegracion;

// HU11 — pruebas de integración del endpoint
// DELETE /api/usuarios/participantes/perfil. Cubren:
//  * Autorización por rol (401/403): anónimo, Administrador y Operador.
//  * Participante Activo elimina su propia cuenta y la base de datos queda
//    sin Usuario / Persona / Participante asociados (cascada EF).
//  * Participante Inactivo recibe error controlado y la base queda intacta.
//  * Si Keycloak falla, la base no queda eliminada.
//  * La respuesta no contiene datos personales.
//
// IMPORTANTE: cada prueba que muta la base usa un participante propio
// (creado en arrange) para no romper otras pruebas de integración que
// dependen del Participante sembrado en FabricaApiPruebas.
public class EliminarCuentaParticipanteEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public EliminarCuentaParticipanteEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private HttpRequestMessage Delete(string? rol, string? idKeycloak)
    {
        var solicitud = new HttpRequestMessage(
            HttpMethod.Delete, "/api/usuarios/participantes/perfil");
        if (rol is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraRol, rol);
        if (idKeycloak is not null)
            solicitud.Headers.Add(AuthHandlerPruebas.CabeceraIdKeycloak, idKeycloak);
        return solicitud;
    }

    // Inserta un Participante (Activo o Inactivo) propio para la prueba.
    // Devuelve el IdKeycloak para que la prueba lo use en el header.
    //
    // Los datos sembrados deben pasar la reconstrucción del agregado de
    // dominio: nombres solo con letras, teléfono de 11 dígitos con código
    // venezolano válido, nombre de usuario con regex de NombreUsuario.
    // `sufijo` debe ser solo letras y `indice` se usa para que el teléfono
    // sea único entre pruebas (índice único en BD).
    private string SembrarParticipanteAislado(EstadoUsuario estado, string sufijo, int indice)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var ahora = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        var nac = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var idUsuario = Guid.NewGuid();
        var idKeycloak = $"kc-hu11-{sufijo}";
        // Teléfono: prefijo válido (0414) + 7 dígitos del índice; total 11.
        var telefono = $"0414{indice:D7}";

        contexto.Usuarios.Add(new UsuarioModelo
        {
            Id = idUsuario,
            NombreUsuario = $"hu11_{sufijo}",
            IdKeycloak = idKeycloak,
            Rol = (int)RolUsuario.Participante,
            Estado = (int)estado,
            FechaRegistro = ahora
        });
        var persona = new PersonaModelo
        {
            Id = Guid.NewGuid(),
            UsuarioId = idUsuario,
            Nombre = "Hache",
            Apellido = "UnoUno",
            Correo = $"hu11_{sufijo}@umbral.com",
            Direccion = "Av. Caracas, Caracas",
            Telefono = telefono,
            Sexo = (int)SexoPersona.Masculino,
            FechaNacimiento = nac,
            FechaRegistro = ahora
        };
        contexto.Personas.Add(persona);
        contexto.Participantes.Add(new ParticipanteModelo
        {
            Id = Guid.NewGuid(),
            PersonaId = persona.Id,
            Alias = $"hu11_{sufijo}",
            FechaRegistro = ahora
        });
        contexto.SaveChanges();
        return idKeycloak;
    }

    private (int usuarios, int personas, int participantes) ContarPorIdKeycloak(string idKeycloak)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
        var usuario = contexto.Usuarios.AsNoTracking()
            .FirstOrDefault(u => u.IdKeycloak == idKeycloak);
        if (usuario is null) return (0, 0, 0);
        var personas = contexto.Personas.AsNoTracking()
            .Count(p => p.UsuarioId == usuario.Id);
        var personaIds = contexto.Personas.AsNoTracking()
            .Where(p => p.UsuarioId == usuario.Id).Select(p => p.Id).ToList();
        var participantes = contexto.Participantes.AsNoTracking()
            .Count(p => personaIds.Contains(p.PersonaId));
        return (1, personas, participantes);
    }

    [Fact]
    public async Task Delete_SinToken_Retorna401()
    {
        var respuesta = await _cliente.SendAsync(Delete(rol: null, idKeycloak: null));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_TokenAdministrador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Delete("Administrador", FabricaApiPruebas.IdKeycloakParticipanteSembrado));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_TokenOperador_Retorna403()
    {
        var respuesta = await _cliente.SendAsync(
            Delete("Operador", FabricaApiPruebas.IdKeycloakParticipanteSembrado));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ParticipanteActivo_EliminaSuPropiaCuenta()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var idKeycloak = SembrarParticipanteAislado(EstadoUsuario.Activo, "ok", indice: 11);

        var respuesta = await _cliente.SendAsync(Delete("Participante", idKeycloak));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<EliminarCuentaParticipanteRespuestaDto>();
        dto!.Eliminada.Should().BeTrue();
        dto.Mensaje.Should().NotBeNullOrWhiteSpace();

        // Verifica que la fila se borró en cascada (Usuario → Persona → Participante).
        var conteo = ContarPorIdKeycloak(idKeycloak);
        conteo.usuarios.Should().Be(0);
        conteo.personas.Should().Be(0);
        conteo.participantes.Should().Be(0);

        // El proveedor recibe el sub del token, no un id del cliente.
        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            idKeycloak, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ParticipanteInactivo_RetornaErrorYNoBorraBase()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        var idKeycloak = SembrarParticipanteAislado(EstadoUsuario.Inactivo, "inactivo", indice: 12);

        var respuesta = await _cliente.SendAsync(Delete("Participante", idKeycloak));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().Contain("CUENTA_DESACTIVADA");

        // Base intacta y Keycloak no fue invocado.
        var conteo = ContarPorIdKeycloak(idKeycloak);
        conteo.usuarios.Should().Be(1);
        conteo.personas.Should().Be(1);
        conteo.participantes.Should().Be(1);
        _fabrica.MockProveedor.Verify(p => p.EliminarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_SubNoCorrespondeAParticipante_Retorna404()
    {
        var respuesta = await _cliente.SendAsync(
            Delete("Participante", "kc-no-existe-hu11"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_KeycloakFalla_NoBorraBaseDatos()
    {
        _fabrica.MockProveedor.Invocations.Clear();
        _fabrica.MockProveedor
            .Setup(p => p.EliminarUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak caído"));

        var idKeycloak = SembrarParticipanteAislado(EstadoUsuario.Activo, "fallakc", indice: 13);

        var respuesta = await _cliente.SendAsync(Delete("Participante", idKeycloak));

        // El manejador no envuelve la excepción; el middleware general la
        // convierte en 500 con un cuerpo controlado (sin stacktrace). No es
        // ni el cuerpo de Keycloak ni el detalle interno: sólo el código y
        // mensaje genéricos.
        ((int)respuesta.StatusCode).Should().Be((int)HttpStatusCode.InternalServerError);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().NotContain("Keycloak caído");

        // Base intacta: la transacción NO se confirmó.
        var conteo = ContarPorIdKeycloak(idKeycloak);
        conteo.usuarios.Should().Be(1);
        conteo.personas.Should().Be(1);
        conteo.participantes.Should().Be(1);

        // Restauramos el mock para el resto de pruebas que usan la misma fábrica.
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

        var idKeycloak = SembrarParticipanteAislado(EstadoUsuario.Activo, "nosens", indice: 14);

        var respuesta = await _cliente.SendAsync(Delete("Participante", idKeycloak));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadAsStringAsync();
        json.Should().NotContain("hu11_nosens@umbral.com");
        json.Should().NotContain("hu11_nosens");
        json.Should().NotContain(idKeycloak);
    }
}
