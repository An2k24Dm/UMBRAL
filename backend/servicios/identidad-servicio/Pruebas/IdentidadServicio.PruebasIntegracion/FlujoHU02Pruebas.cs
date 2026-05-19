using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

// HU02: el Operador creado por el Administrador debe poder iniciar sesión por
// el flujo de HU01 (POST /api/autenticacion/login-web).
public class FlujoHU02Pruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;
    private readonly HttpClient _cliente;

    public FlujoHU02Pruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task CrearOperadorComoAdmin_LuegoLoginWeb_Retorna200()
    {
        var idKc = "kc-op-" + Guid.NewGuid().ToString("N");

        _fabrica.MockProveedor
            .Setup(p => p.CrearUsuarioAsync(
                It.IsAny<DatosCreacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(idKc);
        _fabrica.MockProveedor
            .Setup(p => p.AsignarRolAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var nombreUsuario = "ophu02" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var dto = new CrearUsuarioDto
        {
            TipoUsuario = TipoUsuario.Operador,
            NombreUsuario = nombreUsuario,
            Correo = $"{nombreUsuario}@umbral.com",
            Contrasena = "Abc1*",
            Nombre = "Olivia",
            Apellido = "Operadora",
            Sexo = "Femenino",
            FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DatosContacto = new DatosContactoDto
            {
                Direccion = "Caracas",
                Telefono = "0414" + new Random().Next(1000000, 9999999).ToString()
            }
        };

        var solicitudCreacion = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(dto)
        };
        solicitudCreacion.Headers.Add(AuthHandlerPruebas.CabeceraRol, "Administrador");
        var respuestaCrear = await _cliente.SendAsync(solicitudCreacion);
        respuestaCrear.StatusCode.Should().Be(HttpStatusCode.Created);

        _fabrica.MockProveedor
            .Setup(p => p.IniciarSesionAsync(nombreUsuario, "Abc1*", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoAutenticacionExterna("acc", "ref", 300, "Bearer", idKc));

        var respuestaLogin = await _cliente.PostAsJsonAsync("/api/autenticacion/login-web",
            new InicioSesionDto { NombreUsuario = nombreUsuario, Contrasena = "Abc1*" });

        respuestaLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuestaLogin.Content.ReadFromJsonAsync<ResultadoInicioSesionDto>();
        cuerpo!.Usuario.Rol.Should().Be("Operador");
        cuerpo.RutaRedireccion.Should().Be("/operador/sesiones");
    }
}
