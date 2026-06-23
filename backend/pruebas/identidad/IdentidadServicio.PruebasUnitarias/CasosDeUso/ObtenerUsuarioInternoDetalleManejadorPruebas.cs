using FluentAssertions;
using IdentidadServicio.Aplicacion.Consultas.ObtenerUsuarioInternoDetalle;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.CasosDeUso;

// Pruebas unitarias del manejador de detalle de usuario interno (HU08).
// El manejador delega en el puerto ObtenerUsuarioInternoPorIdAsync (que ya
// excluye Participantes en infraestructura) y, si encuentra un Usuario,
// utiliza la fábrica de estrategias de mapeo de perfil para producir el DTO
// concreto (PerfilOperadorDto / PerfilAdministradorDto). Si el puerto no
// encuentra al usuario o el id corresponde a un Participante, el puerto
// devuelve null y el manejador refleja ese null (la traducción a 404 vive
// en el controlador, fuera del alcance de estas pruebas unitarias).
public class ObtenerUsuarioInternoDetalleManejadorPruebas
{
    private readonly Mock<IRepositorioUsuariosLectura> _repositorio = new();

    private static FabricaEstrategiaMapeoPerfilUsuario CrearFabricaMapeo() =>
        new(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilParticipante()
        });

    private ObtenerUsuarioInternoDetalleManejador CrearManejador() =>
        new(_repositorio.Object, CrearFabricaMapeo());

    private void ConfigurarRepositorio(Guid id, Usuario? usuario)
    {
        _repositorio
            .Setup(r => r.ObtenerUsuarioInternoPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
    }

    // =================================================================
    // Caso 1 — el puerto devuelve un Operador: el manejador produce
    // un PerfilOperadorDto con CodigoOperador y todos los datos comunes.
    // =================================================================
    [Fact]
    public async Task Caso1_ConOperador_DevuelvePerfilOperadorDtoConDatosComunes()
    {
        var id = Guid.NewGuid();
        var operador = UsuariosDePrueba.NuevoOperador(
            nombreUsuario: "operador01",
            correo: "operador@umbral.com",
            nombre: "Olivia",
            apellido: "Operadora",
            codigoOperador: "OP-001");
        ConfigurarRepositorio(id, operador);

        var resultado = await CrearManejador()
            .Handle(new ObtenerUsuarioInternoDetalleConsulta(id), CancellationToken.None);

        resultado.Should().BeOfType<PerfilOperadorDto>();
        var dto = (PerfilOperadorDto)resultado!;

        // Campo específico de Operador.
        dto.CodigoOperador.Should().Be("OP-001");

        // Datos comunes (mismos que en HU05/HU06).
        dto.Id.Should().Be(operador.Id);
        dto.NombreUsuario.Should().Be("operador01");
        dto.Correo.Should().Be("operador@umbral.com");
        dto.Rol.Should().Be("Operador");
        dto.Estado.Should().Be("Activo");
        dto.Nombre.Should().Be("Olivia");
        dto.Apellido.Should().Be("Operadora");
        dto.DatosContacto.Should().NotBeNull();
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
        dto.Sexo.Should().Be("Femenino");
        dto.FechaNacimiento.Should().Be(UsuariosDePrueba.FechaNacimiento);
        dto.FechaRegistro.Should().Be(UsuariosDePrueba.FechaRegistro);
    }

    // =================================================================
    // Caso 2 — el puerto devuelve un Administrador: el manejador produce
    // un PerfilAdministradorDto con CodigoAdministrador y datos comunes.
    // =================================================================
    [Fact]
    public async Task Caso2_ConAdministrador_DevuelvePerfilAdministradorDtoConDatosComunes()
    {
        var id = Guid.NewGuid();
        var admin = UsuariosDePrueba.NuevoAdministrador(
            nombreUsuario: "administrador01",
            correo: "admin@umbral.com",
            nombre: "Ada",
            apellido: "Admin",
            codigoAdministrador: "AD-001");
        ConfigurarRepositorio(id, admin);

        var resultado = await CrearManejador()
            .Handle(new ObtenerUsuarioInternoDetalleConsulta(id), CancellationToken.None);

        resultado.Should().BeOfType<PerfilAdministradorDto>();
        var dto = (PerfilAdministradorDto)resultado!;

        // Campo específico de Administrador.
        dto.CodigoAdministrador.Should().Be("AD-001");

        // Datos comunes.
        dto.Id.Should().Be(admin.Id);
        dto.NombreUsuario.Should().Be("administrador01");
        dto.Correo.Should().Be("admin@umbral.com");
        dto.Rol.Should().Be("Administrador");
        dto.Estado.Should().Be("Activo");
        dto.Nombre.Should().Be("Ada");
        dto.Apellido.Should().Be("Admin");
        dto.DatosContacto.Should().NotBeNull();
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
        dto.Sexo.Should().Be("Femenino");
        dto.FechaNacimiento.Should().Be(UsuariosDePrueba.FechaNacimiento);
        dto.FechaRegistro.Should().Be(UsuariosDePrueba.FechaRegistro);
    }

    // =================================================================
    // Caso 3 — el puerto devuelve null porque no encontró al usuario.
    //
    // El manejador refleja ese null tal cual (es la implementación actual:
    // el controlador es el que traduce a HTTP 404 con "USUARIO_NO_ENCONTRADO").
    // No se modifica lógica productiva para esta prueba.
    // =================================================================
    [Fact]
    public async Task Caso3_PuertoDevuelveNull_ManejadorDevuelveNull()
    {
        var id = Guid.NewGuid();
        ConfigurarRepositorio(id, usuario: null);

        var resultado = await CrearManejador()
            .Handle(new ObtenerUsuarioInternoDetalleConsulta(id), CancellationToken.None);

        resultado.Should().BeNull();
    }

    // =================================================================
    // Caso 4 — exclusión de Participante.
    //
    // En la implementación actual, la exclusión vive en el repositorio
    // (ObtenerUsuarioInternoPorIdAsync devuelve null para un Participante).
    // El manejador refleja esa decisión: si pidiéramos por un id que en la
    // base es un Participante, el mock devolverá null y el manejador
    // devuelve null. Lo que importa para HU08 es que el manejador NUNCA
    // consulta por un puerto genérico que sí cargaría Participantes
    // (p. ej. ObtenerPorIdKeycloakAsync) y por tanto nunca puede producir
    // un PerfilParticipanteDto.
    // =================================================================
    [Fact]
    public async Task Caso4_IdDeParticipante_PuertoDevuelveNullYManejadorRechaza()
    {
        // El puerto, en producción, ya filtra Participantes: simulamos su
        // contrato devolviendo null para un id que en la base es Participante.
        var idParticipante = Guid.NewGuid();
        ConfigurarRepositorio(idParticipante, usuario: null);

        var resultado = await CrearManejador()
            .Handle(new ObtenerUsuarioInternoDetalleConsulta(idParticipante), CancellationToken.None);

        // Bajo ninguna circunstancia el manejador devuelve PerfilParticipanteDto.
        resultado.Should().BeNull();
        // Refuerzo explícito: el resultado no es un perfil de Participante.
        (resultado is PerfilParticipanteDto).Should().BeFalse();
    }

    [Fact]
    public async Task Caso4b_NoConsultaPuertosGenericosQueIncluyenParticipantes()
    {
        // Garantía estructural: si alguien cambiara el manejador para usar
        // un método genérico (que sí incluye Participantes), esta prueba
        // fallaría. Es la salvaguarda de HU08 a nivel de manejador.
        var id = Guid.NewGuid();
        ConfigurarRepositorio(id, UsuariosDePrueba.NuevoOperador());

        await CrearManejador()
            .Handle(new ObtenerUsuarioInternoDetalleConsulta(id), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerUsuarioInternoPorIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(
            r => r.ObtenerPorIdKeycloakAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repositorio.Verify(
            r => r.ObtenerPorNombreUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =================================================================
    // Caso 5 — el manejador consulta al puerto exactamente con el Id recibido.
    // =================================================================
    [Fact]
    public async Task Caso5_ConsultaPorElIdRecibidoEnLaConsulta()
    {
        var id = Guid.NewGuid();
        ConfigurarRepositorio(id, UsuariosDePrueba.NuevoAdministrador());

        await CrearManejador()
            .Handle(new ObtenerUsuarioInternoDetalleConsulta(id), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerUsuarioInternoPorIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
