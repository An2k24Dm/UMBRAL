using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.CasosDeUso;

public class ObtenerPerfilActualManejadorPruebas
{
    private readonly Mock<IRepositorioUsuariosLectura> _repositorio = new();

    private static FabricaEstrategiaMapeoPerfilUsuario CrearFabricaMapeo() =>
        new(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilParticipante()
        });

    private ObtenerPerfilActualManejador CrearManejador() =>
        new(_repositorio.Object, CrearFabricaMapeo());

    private void ConfigurarRepositorio(string idKeycloak, Usuario? usuario)
    {
        _repositorio
            .Setup(r => r.ObtenerPorIdKeycloakAsync(idKeycloak, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
    }

    [Fact]
    public async Task Handle_ConOperador_DevuelvePerfilOperadorDto()
    {
        const string idKc = "kc-operador";
        var operador = UsuariosDePrueba.NuevoOperador(codigoOperador: "OP-042");
        ConfigurarRepositorio(idKc, operador);

        var resultado = await CrearManejador()
            .Handle(new ObtenerPerfilActualConsulta(idKc), CancellationToken.None);

        resultado.Should().BeOfType<PerfilOperadorDto>();
        var dto = (PerfilOperadorDto)resultado;
        dto.CodigoOperador.Should().Be("OP-042");
        dto.Rol.Should().Be("Operador");
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
    }

    [Fact]
    public async Task Handle_ConAdministrador_DevuelvePerfilAdministradorDto()
    {
        const string idKc = "kc-administrador";
        var administrador = UsuariosDePrueba.NuevoAdministrador(codigoAdministrador: "AD-007");
        ConfigurarRepositorio(idKc, administrador);

        var resultado = await CrearManejador()
            .Handle(new ObtenerPerfilActualConsulta(idKc), CancellationToken.None);

        resultado.Should().BeOfType<PerfilAdministradorDto>();
        var dto = (PerfilAdministradorDto)resultado;
        dto.CodigoAdministrador.Should().Be("AD-007");
        dto.Rol.Should().Be("Administrador");
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
    }

    [Fact]
    public async Task Handle_ConParticipante_DevuelvePerfilParticipanteDto()
    {
        const string idKc = "kc-participante";
        var participante = UsuariosDePrueba.NuevoParticipante(alias: "pablito");
        ConfigurarRepositorio(idKc, participante);

        var resultado = await CrearManejador()
            .Handle(new ObtenerPerfilActualConsulta(idKc), CancellationToken.None);

        resultado.Should().BeOfType<PerfilParticipanteDto>();
        var dto = (PerfilParticipanteDto)resultado;
        dto.Alias.Should().Be("pablito");
        dto.Rol.Should().Be("Participante");
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
    }

    [Fact]
    public async Task Handle_SinUsuarioRegistrado_LanzaDatosUsuarioInvalidos()
    {
        const string idKc = "kc-inexistente";
        ConfigurarRepositorio(idKc, usuario: null);

        Func<Task> accion = async () => await CrearManejador()
            .Handle(new ObtenerPerfilActualConsulta(idKc), CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*no registrado*");
    }

    [Fact]
    public async Task Handle_ConsultaPorIdKeycloak()
    {
        const string idKc = "kc-buscado";
        ConfigurarRepositorio(idKc, UsuariosDePrueba.NuevoOperador());

        await CrearManejador()
            .Handle(new ObtenerPerfilActualConsulta(idKc), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerPorIdKeycloakAsync(idKc, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
