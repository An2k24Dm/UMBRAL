using IdentidadServicio.Aplicacion.Consultas.ConsultarParticipantes;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

namespace IdentidadServicio.PruebasUnitarias.CasosDeUso;

// HU07: pruebas del manejador de consulta paginada de Participantes.
// Tras el refactor el manejador depende solo de IRepositorioParticipantes.
public class ConsultarParticipantesManejadorPruebas
{
    private readonly Mock<IRepositorioParticipantes> _repositorio = new();

    private ConsultarParticipantesManejador CrearManejador() => new(_repositorio.Object);

    private void ConfigurarRepositorio(
        IReadOnlyList<Participante> participantes,
        int total,
        int paginaEsperada,
        int tamanioEsperado,
        string? ordenEsperado)
    {
        _repositorio
            .Setup(r => r.ConsultarAsync(
                paginaEsperada, tamanioEsperado, ordenEsperado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participantes);
        _repositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(total);
    }

    [Fact]
    public async Task Handle_DevuelveResultadoPaginadoConElementosYTotal()
    {
        var participantes = new List<Participante>
        {
            UsuariosDePrueba.NuevoParticipante(nombreUsuario: "participante01", alias: "sombra01"),
            UsuariosDePrueba.NuevoParticipante(nombreUsuario: "participante02", alias: "sombra02")
        };
        ConfigurarRepositorio(participantes, total: 2, paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: null);

        var resultado = await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, null), CancellationToken.None);

        resultado.Elementos.Should().HaveCount(2);
        resultado.Pagina.Should().Be(1);
        resultado.TamanioPagina.Should().Be(10);
        resultado.Total.Should().Be(2);
        resultado.Elementos[0].NombreUsuario.Should().Be("participante01");
        resultado.Elementos[0].Alias.Should().Be("sombra01");
    }

    [Fact]
    public async Task Handle_ConParametrosPorDefecto_NormalizaPaginaYTamanio()
    {
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 0,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: null);

        var resultado = await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(0, 0, null), CancellationToken.None);

        resultado.Pagina.Should().Be(1);
        resultado.TamanioPagina.Should().Be(10);
        _repositorio.Verify(r => r.ConsultarAsync(
            1, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConPagina2_LlamaAlRepositorioConPagina2()
    {
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 15,
            paginaEsperada: 2, tamanioEsperado: 10, ordenEsperado: null);

        await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(2, 10, null), CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarAsync(
            2, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("ASC")]
    [InlineData("  asc  ")]
    public async Task Handle_ConOrdenEstadoAsc_NormalizaYPropaga(string entrada)
    {
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 0,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: "asc");

        await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, entrada), CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarAsync(
            1, 10, "asc", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConOrdenEstadoDesc_PropagaDescAlRepositorio()
    {
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 0,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: "desc");

        await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, "desc"), CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarAsync(
            1, 10, "desc", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConOrdenEstadoInvalido_LoConvierteEnNull()
    {
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 0,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: null);

        await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, "cualquier-cosa"), CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarAsync(
            1, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SinResultados_DevuelveListaVaciaYTotalCero()
    {
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 0,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: null);

        var resultado = await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, null), CancellationToken.None);

        resultado.Elementos.Should().BeEmpty();
        resultado.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_LosElementosSoloContienenCamposDeParticipante()
    {
        var participante = UsuariosDePrueba.NuevoParticipante(
            nombreUsuario: "participante01",
            nombre: "Pablo",
            apellido: "Participante",
            alias: "sombra01");
        ConfigurarRepositorio(new[] { participante }, total: 1,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: null);

        var resultado = await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, null), CancellationToken.None);

        var dto = resultado.Elementos.Single();
        dto.Id.Should().Be(participante.Id);
        dto.Alias.Should().Be("sombra01");
        dto.NombreUsuario.Should().Be("participante01");
        dto.Nombre.Should().Be("Pablo");
        dto.Apellido.Should().Be("Participante");
        dto.Estado.Should().Be("Activo");
        dto.Sexo.Should().Be("Masculino");
    }

    [Fact]
    public async Task Handle_NoConsultaOperadoresNiAdministradores()
    {
        // El manejador depende SOLO de IRepositorioParticipantes — al estar
        // tipado por la interfaz específica, ni siquiera podría tocar
        // operadores o administradores. La prueba confirma que solo se llama
        // a ConsultarAsync / ContarAsync.
        ConfigurarRepositorio(Array.Empty<Participante>(), total: 0,
            paginaEsperada: 1, tamanioEsperado: 10, ordenEsperado: null);

        await CrearManejador()
            .Handle(new ConsultarParticipantesConsulta(1, 10, null), CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(r => r.ObtenerPorIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
