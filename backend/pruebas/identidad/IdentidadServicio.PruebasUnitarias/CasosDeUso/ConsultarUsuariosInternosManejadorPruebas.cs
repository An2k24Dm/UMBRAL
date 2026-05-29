using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.CasosDeUso;

// Pruebas unitarias del manejador de la consulta paginada de cuentas internas
// (HU08). El manejador:
//   1) normaliza los parámetros (pagina, tamaño fijo, rol, orden),
//   2) pide al repositorio entidades de dominio (Operador / Administrador),
//   3) arma el ResultadoPaginadoDto<UsuarioInternoListadoDto>.
// La exclusión de Participantes recae en el puerto, pero el manejador también
// descarta defensivamente cualquier entidad no soportada.
public class ConsultarUsuariosInternosManejadorPruebas
{
    // Tamaño de página fijado por HU08.
    private const int TamanioPaginaHu08 = 10;

    private readonly Mock<IRepositorioUsuariosLectura> _repositorio = new();

    private ConsultarUsuariosInternosManejador CrearManejador() => new(_repositorio.Object);

    private void ConfigurarRepositorio(
        IReadOnlyList<Usuario> usuarios,
        int total)
    {
        _repositorio
            .Setup(r => r.ConsultarUsuariosInternosAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<RolUsuario?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarios);
        _repositorio
            .Setup(r => r.ContarUsuariosInternosAsync(
                It.IsAny<RolUsuario?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(total);
    }

    private void ConfigurarRepositorioVacio() =>
        ConfigurarRepositorio(Array.Empty<Usuario>(), 0);

    // =================================================================
    // Caso 1 — rol = Todos: devuelve Operadores y Administradores,
    // nunca Participantes, en una página de 10 a partir de la página 1.
    // =================================================================
    [Fact]
    public async Task Caso1_FiltroTodos_DevuelveOperadoresYAdministradoresPaginados()
    {
        var usuarios = new Usuario[]
        {
            UsuariosDePrueba.NuevoOperador("operador01", codigoOperador: "OP-001"),
            UsuariosDePrueba.NuevoAdministrador("administrador01", codigoAdministrador: "AD-001"),
            UsuariosDePrueba.NuevoOperador("operador02", codigoOperador: "OP-002")
        };
        ConfigurarRepositorio(usuarios, total: 3);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado.Pagina.Should().Be(1);
        resultado.TamanioPagina.Should().Be(TamanioPaginaHu08);
        resultado.Total.Should().Be(3);
        resultado.Elementos.Should().HaveCount(3);

        // Solo roles internos: ningún Participante.
        resultado.Elementos.Should().OnlyContain(
            e => e.Rol == nameof(RolUsuario.Operador)
              || e.Rol == nameof(RolUsuario.Administrador));
        resultado.Elementos.Should().NotContain(
            e => e.Rol == nameof(RolUsuario.Participante));

        // El manejador pidió al puerto un filtro "Todos" → rol = null.
        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            1,
            TamanioPaginaHu08,
            (RolUsuario?)null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ContarUsuariosInternosAsync(
            (RolUsuario?)null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 2 — rol = Operador: el filtro viaja como RolUsuario.Operador
    // y los elementos devueltos tienen CodigoOperador como código principal.
    // =================================================================
    [Fact]
    public async Task Caso2_FiltroOperador_PropagaElFiltroYDevuelveFilasConCodigoOperador()
    {
        var usuarios = new Usuario[]
        {
            UsuariosDePrueba.NuevoOperador("operador01", codigoOperador: "OP-001"),
            UsuariosDePrueba.NuevoOperador("operador02", codigoOperador: "OP-002")
        };
        ConfigurarRepositorio(usuarios, total: 2);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Operador", null),
                    CancellationToken.None);

        resultado.Elementos.Should().OnlyContain(e => e.Rol == nameof(RolUsuario.Operador));
        resultado.Elementos.Should().OnlyContain(e => !string.IsNullOrEmpty(e.CodigoOperador));
        // En filas de Operador, el código de administrador queda en null.
        resultado.Elementos.Should().OnlyContain(e => string.IsNullOrEmpty(e.CodigoAdministrador));

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            RolUsuario.Operador,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ContarUsuariosInternosAsync(
            RolUsuario.Operador,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 3 — rol = Administrador: análogo al caso 2 pero para Administradores.
    // =================================================================
    [Fact]
    public async Task Caso3_FiltroAdministrador_PropagaElFiltroYDevuelveFilasConCodigoAdministrador()
    {
        var usuarios = new Usuario[]
        {
            UsuariosDePrueba.NuevoAdministrador("administrador01", codigoAdministrador: "AD-001"),
            UsuariosDePrueba.NuevoAdministrador("administrador02", codigoAdministrador: "AD-002")
        };
        ConfigurarRepositorio(usuarios, total: 2);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Administrador", null),
                    CancellationToken.None);

        resultado.Elementos.Should().OnlyContain(e => e.Rol == nameof(RolUsuario.Administrador));
        resultado.Elementos.Should().OnlyContain(e => !string.IsNullOrEmpty(e.CodigoAdministrador));
        // En filas de Administrador, el código de operador queda en null.
        resultado.Elementos.Should().OnlyContain(e => string.IsNullOrEmpty(e.CodigoOperador));

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            RolUsuario.Administrador,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ContarUsuariosInternosAsync(
            RolUsuario.Administrador,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 4 — ordenEstado = "asc" se delega al puerto en minúsculas.
    // =================================================================
    [Fact]
    public async Task Caso4_OrdenAsc_SePropagaAlRepositorio()
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", "asc"),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RolUsuario?>(),
            "asc",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 5 — ordenEstado = "desc" se delega al puerto en minúsculas.
    // =================================================================
    [Fact]
    public async Task Caso5_OrdenDesc_SePropagaAlRepositorio()
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", "desc"),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RolUsuario?>(),
            "desc",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Variantes adicionales: la normalización vuelve "asc"/"desc" minúsculas
    // y descarta valores no reconocidos. Cubre el contrato del manejador.
    [Theory]
    [InlineData("ASC", "asc")]
    [InlineData("Asc", "asc")]
    [InlineData("DESC", "desc")]
    [InlineData("xxx", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public async Task Caso5b_NormalizaOrdenAntesDeLlamarAlRepositorio(
        string? entrada, string? esperado)
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", entrada),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RolUsuario?>(),
            esperado,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 6 — sin resultados: lista vacía, total 0, sin excepción.
    // =================================================================
    [Fact]
    public async Task Caso6_SinResultados_DevuelveListaVaciaYTotalCero()
    {
        ConfigurarRepositorioVacio();

        var accion = async () => await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        var resultado = await accion.Should().NotThrowAsync();
        resultado.Subject!.Elementos.Should().BeEmpty();
        resultado.Subject.Total.Should().Be(0);
        resultado.Subject.Pagina.Should().Be(1);
        resultado.Subject.TamanioPagina.Should().Be(TamanioPaginaHu08);
    }

    // =================================================================
    // Caso 7 — el manejador llama al puerto con pagina, tamanioPagina,
    // rol y ordenEstado correctamente traducidos.
    // =================================================================
    [Fact]
    public async Task Caso7_PropagaPaginaTamanioRolYOrdenAlRepositorio()
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(2, TamanioPaginaHu08, "Operador", "asc"),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            2,
            TamanioPaginaHu08,
            RolUsuario.Operador,
            "asc",
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ContarUsuariosInternosAsync(
            RolUsuario.Operador,
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.VerifyNoOtherCalls();
    }

    // =================================================================
    // El total devuelto al cliente proviene de ContarUsuariosInternosAsync,
    // independientemente de cuántos elementos traiga la página actual.
    // =================================================================
    [Fact]
    public async Task Total_VieneDeContarUsuariosInternosAsync()
    {
        var usuarios = new Usuario[]
        {
            UsuariosDePrueba.NuevoOperador("operador01", codigoOperador: "OP-001")
        };
        ConfigurarRepositorio(usuarios, total: 27);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        resultado.Total.Should().Be(27);
        resultado.Elementos.Should().HaveCount(1);
    }

    // =================================================================
    // El mapeo de Operador produce CodigoOperador y CodigoAdministrador = null,
    // y el de Administrador produce el caso simétrico.
    // =================================================================
    [Fact]
    public async Task MapeoOperadorYAdministrador_ProduceCodigosCorrectos()
    {
        var operador = UsuariosDePrueba.NuevoOperador(
            nombreUsuario: "operador01", codigoOperador: "OP-007");
        var administrador = UsuariosDePrueba.NuevoAdministrador(
            nombreUsuario: "administrador01", codigoAdministrador: "AD-007");
        ConfigurarRepositorio(new Usuario[] { operador, administrador }, total: 2);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        var filaOperador = resultado.Elementos.Single(e => e.Id == operador.Id);
        filaOperador.Rol.Should().Be(nameof(RolUsuario.Operador));
        filaOperador.CodigoOperador.Should().Be("OP-007");
        filaOperador.CodigoAdministrador.Should().BeNull();
        filaOperador.NombreUsuario.Should().Be("operador01");
        filaOperador.Nombre.Should().Be("Olivia");
        filaOperador.Apellido.Should().Be("Operadora");
        filaOperador.Estado.Should().Be("Activo");
        filaOperador.Sexo.Should().Be("Femenino");

        var filaAdmin = resultado.Elementos.Single(e => e.Id == administrador.Id);
        filaAdmin.Rol.Should().Be(nameof(RolUsuario.Administrador));
        filaAdmin.CodigoAdministrador.Should().Be("AD-007");
        filaAdmin.CodigoOperador.Should().BeNull();
        filaAdmin.NombreUsuario.Should().Be("administrador01");
        filaAdmin.Nombre.Should().Be("Ada");
        filaAdmin.Apellido.Should().Be("Admin");
        filaAdmin.Estado.Should().Be("Activo");
        filaAdmin.Sexo.Should().Be("Femenino");
    }

    // =================================================================
    // Si el repositorio devolviera defensivamente un Participante, el
    // manejador lo descarta sin romper el resto del listado.
    // =================================================================
    [Fact]
    public async Task Mapeo_DescartaParticipantesDefensivamente()
    {
        var operador = UsuariosDePrueba.NuevoOperador("operador01", codigoOperador: "OP-001");
        var participante = UsuariosDePrueba.NuevoParticipante("participante01");
        ConfigurarRepositorio(new Usuario[] { operador, participante }, total: 2);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        resultado.Elementos.Should().HaveCount(1);
        resultado.Elementos.Should().OnlyContain(e => e.Rol == nameof(RolUsuario.Operador));
        resultado.Elementos.Should().NotContain(e => e.Rol == nameof(RolUsuario.Participante));
        // El total proviene del puerto, no se recalcula a partir del filtrado defensivo.
        resultado.Total.Should().Be(2);
    }

    // =================================================================
    // HU08 fija el tamaño de página: el cliente puede enviar otro valor,
    // pero el caso de uso siempre opera con 10.
    // =================================================================
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task TamanioPagina_SiempreSeFijaA10(int tamanioCliente)
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, tamanioCliente, "Todos", null),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            TamanioPaginaHu08,
            It.IsAny<RolUsuario?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Cualquier número de página menor a 1 se normaliza a 1.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99)]
    public async Task PaginaInvalida_SeNormalizaA1(int paginaCliente)
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(paginaCliente, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            1,
            It.IsAny<int>(),
            It.IsAny<RolUsuario?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Paginación: si el cliente pide la página 2, el manejador la respeta.
    [Fact]
    public async Task Pagina2_SeRespetaAlConsultarElRepositorio()
    {
        ConfigurarRepositorio(
            new Usuario[] { UsuariosDePrueba.NuevoOperador("operador11", codigoOperador: "OP-011") },
            total: 11);

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(2, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        resultado.Pagina.Should().Be(2);
        resultado.Total.Should().Be(11);
        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            2,
            TamanioPaginaHu08,
            It.IsAny<RolUsuario?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Exclusión de Participantes — el manejador usa exclusivamente el
    // puerto específico de HU08 (ConsultarUsuariosInternosAsync) y no
    // métodos genéricos del repositorio que sí incluirían Participantes.
    // =================================================================
    [Fact]
    public async Task NoConsultaPuertosGenericosQuePodrianTraerParticipantes()
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerPorIdKeycloakAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repositorio.Verify(
            r => r.ObtenerPorNombreUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // El puerto IRepositorioUsuariosLectura no expone Participantes; tras
        // el refactor del repositorio esa responsabilidad vive en
        // IRepositorioParticipantes, no inyectado en este manejador.
    }

    // Filtros desconocidos (p. ej. "Participante" o cualquier otro valor
    // no soportado) se tratan como "Todos" → rol = null, lo que confirma
    // que el manejador no permite colar Participantes vía el filtro.
    [Theory]
    [InlineData("Participante")]
    [InlineData("participante")]
    [InlineData("desconocido")]
    public async Task FiltroNoReconocido_SeTrataComoTodos(string filtro)
    {
        ConfigurarRepositorioVacio();

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, filtro, null),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            (RolUsuario?)null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.ContarUsuariosInternosAsync(
            (RolUsuario?)null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
