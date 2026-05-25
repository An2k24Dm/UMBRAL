using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

namespace IdentidadServicio.PruebasUnitarias.CasosDeUso;

// HU07: pruebas del manejador de detalle de Participante.
// El puerto del repositorio garantiza que los usuarios internos (Operador,
// Administrador) nunca lleguen a este caso de uso: devuelve null y el
// manejador se traduce a "no encontrado". Estas pruebas verifican ese
// contrato simulándolo con Moq.
public class ObtenerParticipanteDetalleManejadorPruebas
{
    private readonly Mock<IRepositorioParticipantes> _repositorio = new();

    private static FabricaEstrategiaMapeoPerfilUsuario CrearFabricaMapeo() =>
        new(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilParticipante()
        });

    private ObtenerParticipanteDetalleManejador CrearManejador() =>
        new(_repositorio.Object, CrearFabricaMapeo());

    private void ConfigurarRepositorio(Guid id, Participante? participante)
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participante);
    }

    [Fact]
    public async Task Handle_ConParticipante_DevuelvePerfilParticipanteDto()
    {
        var participante = UsuariosDePrueba.NuevoParticipante(
            nombreUsuario: "participante01",
            correo: "participante@umbral.com",
            nombre: "Pablo",
            apellido: "Participante",
            alias: "sombra01");
        ConfigurarRepositorio(participante.Id, participante);

        var resultado = await CrearManejador()
            .Handle(new ObtenerParticipanteDetalleConsulta(participante.Id), CancellationToken.None);

        resultado.Should().BeOfType<PerfilParticipanteDto>();
        resultado.Alias.Should().Be("sombra01");
    }

    [Fact]
    public async Task Handle_IncluyeDatosComunesDelPerfil()
    {
        var participante = UsuariosDePrueba.NuevoParticipante(
            nombreUsuario: "participante01",
            correo: "participante@umbral.com",
            nombre: "Pablo",
            apellido: "Participante",
            alias: "sombra01");
        ConfigurarRepositorio(participante.Id, participante);

        var resultado = await CrearManejador()
            .Handle(new ObtenerParticipanteDetalleConsulta(participante.Id), CancellationToken.None);

        resultado.Id.Should().Be(participante.Id);
        resultado.NombreUsuario.Should().Be("participante01");
        resultado.Correo.Should().Be("participante@umbral.com");
        resultado.Rol.Should().Be("Participante");
        resultado.Estado.Should().Be("Activo");
        resultado.Nombre.Should().Be("Pablo");
        resultado.Apellido.Should().Be("Participante");
        resultado.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        resultado.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
        resultado.Sexo.Should().Be("Masculino");
        resultado.FechaNacimiento.Should().Be(UsuariosDePrueba.FechaNacimiento);
        resultado.FechaRegistro.Should().Be(UsuariosDePrueba.FechaRegistro);
    }

    [Fact]
    public async Task Handle_CuandoElRepositorioDevuelveNull_LanzaDatosUsuarioInvalidos()
    {
        var id = Guid.NewGuid();
        ConfigurarRepositorio(id, participante: null);

        Func<Task> accion = async () => await CrearManejador()
            .Handle(new ObtenerParticipanteDetalleConsulta(id), CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*Participante no encontrado*");
    }

    [Fact]
    public async Task Handle_SiElIdCorrespondeAOperador_RepositorioDevuelveNullYLanza()
    {
        // El repositorio implementa la regla "sólo Participantes". Para HU07
        // ese caso se traduce a null y el manejador a "no encontrado".
        var id = Guid.NewGuid();
        ConfigurarRepositorio(id, participante: null);

        Func<Task> accion = async () => await CrearManejador()
            .Handle(new ObtenerParticipanteDetalleConsulta(id), CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public async Task Handle_SiElIdCorrespondeAAdministrador_RepositorioDevuelveNullYLanza()
    {
        var id = Guid.NewGuid();
        ConfigurarRepositorio(id, participante: null);

        Func<Task> accion = async () => await CrearManejador()
            .Handle(new ObtenerParticipanteDetalleConsulta(id), CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public async Task Handle_ConsultaAlRepositorioPorElIdRecibido()
    {
        var participante = UsuariosDePrueba.NuevoParticipante();
        ConfigurarRepositorio(participante.Id, participante);

        await CrearManejador()
            .Handle(new ObtenerParticipanteDetalleConsulta(participante.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerPorIdAsync(participante.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
