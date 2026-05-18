using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

public class UsuariosEndpointPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public UsuariosEndpointPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private CrearUsuarioDto DtoParticipante() => new()
    {
        TipoUsuario = TipoUsuario.Participante,
        NombreUsuario = ("par" + Guid.NewGuid().ToString("N")).Substring(0, 20),
        Correo = $"{Guid.NewGuid():N}@umbral.com",
        ContrasenaTemporal = "Temporal123*",
        Nombre = "Nuevo",
        Apellido = "Participante",
        Sexo = "Masculino",
        FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto { Direccion = "Calle 1", Telefono = "555" },
        Alias = ("ali" + Guid.NewGuid().ToString("N")).Substring(0, 20)
    };

    [Fact]
    public async Task PostUsuarios_Participante_Retorna201()
    {
        _fabrica.MockProveedor
            .Setup(p => p.CrearUsuarioAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"kc-par-{Guid.NewGuid():N}");
        _fabrica.MockProveedor
            .Setup(p => p.AsignarRolAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var respuesta = await _cliente.PostAsJsonAsync("/api/usuarios", DtoParticipante());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CrearUsuarioRespuestaDto>();
        cuerpo!.Rol.Should().Be("Participante");
        cuerpo.Estado.Should().Be("Activo");
    }

    [Fact]
    public async Task PostUsuarios_NombreUsuarioYCorreoSonSeparados()
    {
        // Verifica que el backend envía a Keycloak el username (NombreUsuario)
        // y el email (Correo) como campos separados.
        _fabrica.MockProveedor
            .Setup(p => p.CrearUsuarioAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-x");
        _fabrica.MockProveedor
            .Setup(p => p.AsignarRolAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = DtoParticipante();
        dto.NombreUsuario = "operador01";
        dto.Correo = "operador@umbral.com";

        var respuesta = await _cliente.PostAsJsonAsync("/api/usuarios", dto);
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        _fabrica.MockProveedor.Verify(p => p.CrearUsuarioAsync(
            "operador01", "operador@umbral.com", "Temporal123*",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
