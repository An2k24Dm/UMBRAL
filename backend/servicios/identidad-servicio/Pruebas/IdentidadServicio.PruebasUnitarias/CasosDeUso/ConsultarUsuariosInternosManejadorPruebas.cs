using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.CasosDeUso;

// Pruebas unitarias del manejador de la consulta paginada de cuentas internas
// (HU08). El manejador es un orquestador delgado: traduce los parámetros
// recibidos al puerto y devuelve el resultado paginado tal cual viene del
// repositorio. Por eso casi todas las pruebas se enfocan en:
//   1) cómo se traducen los parámetros antes de llegar al puerto,
//   2) qué hace el manejador con el resultado del puerto,
//   3) que la exclusión de Participantes ocurre vía el método específico del
//      puerto y no por uno genérico que pudiera devolverlos.
public class ConsultarUsuariosInternosManejadorPruebas
{
    // Tamaño de página fijado por HU08.
    private const int TamanioPaginaHu08 = 10;

    private readonly Mock<IRepositorioIdentidad> _repositorio = new();

    private ConsultarUsuariosInternosManejador CrearManejador() => new(_repositorio.Object);

    private void ConfigurarRepositorio(
        ResultadoPaginadoDto<UsuarioInternoListadoDto> resultado)
    {
        _repositorio
            .Setup(r => r.ConsultarUsuariosInternosAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<RolUsuario?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);
    }

    private static ResultadoPaginadoDto<UsuarioInternoListadoDto> ResultadoVacio(
        int pagina = 1, int tamanio = TamanioPaginaHu08) =>
        new()
        {
            Elementos = Array.Empty<UsuarioInternoListadoDto>(),
            Pagina = pagina,
            TamanioPagina = tamanio,
            Total = 0
        };

    // -----------------------------------------------------------------
    // Construcción de filas de ejemplo (sin DateTime.Now / DateTime.UtcNow).
    // -----------------------------------------------------------------
    private static UsuarioInternoListadoDto FilaOperador(
        string nombreUsuario = "operador01",
        string codigoOperador = "OP-001",
        string estado = "Activo")
        => new()
        {
            Id = Guid.NewGuid(),
            Rol = nameof(RolUsuario.Operador),
            NombreUsuario = nombreUsuario,
            Nombre = "Olivia",
            Apellido = "Operadora",
            Estado = estado,
            Sexo = nameof(SexoPersona.Femenino),
            CodigoOperador = codigoOperador,
            CodigoAdministrador = null
        };

    private static UsuarioInternoListadoDto FilaAdministrador(
        string nombreUsuario = "administrador01",
        string codigoAdministrador = "AD-001",
        string estado = "Activo")
        => new()
        {
            Id = Guid.NewGuid(),
            Rol = nameof(RolUsuario.Administrador),
            NombreUsuario = nombreUsuario,
            Nombre = "Ada",
            Apellido = "Admin",
            Estado = estado,
            Sexo = nameof(SexoPersona.Femenino),
            CodigoOperador = null,
            CodigoAdministrador = codigoAdministrador
        };

    // =================================================================
    // Caso 1 — rol = Todos: devuelve Operadores y Administradores,
    // nunca Participantes, en una página de 10 a partir de la página 1.
    // =================================================================
    [Fact]
    public async Task Caso1_FiltroTodos_DevuelveOperadoresYAdministradoresPaginados()
    {
        var elementos = new[]
        {
            FilaOperador("operador01", "OP-001"),
            FilaAdministrador("administrador01", "AD-001"),
            FilaOperador("operador02", "OP-002")
        };
        ConfigurarRepositorio(new ResultadoPaginadoDto<UsuarioInternoListadoDto>
        {
            Elementos = elementos,
            Pagina = 1,
            TamanioPagina = TamanioPaginaHu08,
            Total = 3
        });

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
    }

    // =================================================================
    // Caso 2 — rol = Operador: el filtro viaja como RolUsuario.Operador
    // y los elementos devueltos tienen CodigoOperador como código principal.
    // =================================================================
    [Fact]
    public async Task Caso2_FiltroOperador_PropagaElFiltroYDevuelveFilasConCodigoOperador()
    {
        var elementos = new[]
        {
            FilaOperador("operador01", "OP-001"),
            FilaOperador("operador02", "OP-002")
        };
        ConfigurarRepositorio(new ResultadoPaginadoDto<UsuarioInternoListadoDto>
        {
            Elementos = elementos,
            Pagina = 1,
            TamanioPagina = TamanioPaginaHu08,
            Total = 2
        });

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Operador", null),
                    CancellationToken.None);

        resultado.Elementos.Should().OnlyContain(e => e.Rol == nameof(RolUsuario.Operador));
        resultado.Elementos.Should().OnlyContain(e => !string.IsNullOrEmpty(e.CodigoOperador));
        // En filas de Operador, el código de administrador no es el código principal.
        resultado.Elementos.Should().OnlyContain(e => string.IsNullOrEmpty(e.CodigoAdministrador));

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            RolUsuario.Operador,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 3 — rol = Administrador: análogo al caso 2 pero para Administradores.
    // =================================================================
    [Fact]
    public async Task Caso3_FiltroAdministrador_PropagaElFiltroYDevuelveFilasConCodigoAdministrador()
    {
        var elementos = new[]
        {
            FilaAdministrador("administrador01", "AD-001"),
            FilaAdministrador("administrador02", "AD-002")
        };
        ConfigurarRepositorio(new ResultadoPaginadoDto<UsuarioInternoListadoDto>
        {
            Elementos = elementos,
            Pagina = 1,
            TamanioPagina = TamanioPaginaHu08,
            Total = 2
        });

        var resultado = await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Administrador", null),
                    CancellationToken.None);

        resultado.Elementos.Should().OnlyContain(e => e.Rol == nameof(RolUsuario.Administrador));
        resultado.Elementos.Should().OnlyContain(e => !string.IsNullOrEmpty(e.CodigoAdministrador));
        // En filas de Administrador, el código de operador no es el código principal.
        resultado.Elementos.Should().OnlyContain(e => string.IsNullOrEmpty(e.CodigoOperador));

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            RolUsuario.Administrador,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // Caso 4 — ordenEstado = "asc" se delega al puerto en minúsculas.
    // =================================================================
    [Fact]
    public async Task Caso4_OrdenAsc_SePropagaAlRepositorio()
    {
        ConfigurarRepositorio(ResultadoVacio());

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
        ConfigurarRepositorio(ResultadoVacio());

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
        ConfigurarRepositorio(ResultadoVacio());

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
        ConfigurarRepositorio(ResultadoVacio());

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
        ConfigurarRepositorio(ResultadoVacio(pagina: 2));

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(2, TamanioPaginaHu08, "Operador", "asc"),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            2,
            TamanioPaginaHu08,
            RolUsuario.Operador,
            "asc",
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.VerifyNoOtherCalls();
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
        ConfigurarRepositorio(ResultadoVacio());

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
        ConfigurarRepositorio(ResultadoVacio());

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
        ConfigurarRepositorio(new ResultadoPaginadoDto<UsuarioInternoListadoDto>
        {
            Elementos = new[] { FilaOperador("operador11") },
            Pagina = 2,
            TamanioPagina = TamanioPaginaHu08,
            Total = 11
        });

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
        ConfigurarRepositorio(ResultadoVacio());

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, "Todos", null),
                    CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerPorIdKeycloakAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repositorio.Verify(
            r => r.ObtenerPorNombreUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        ConfigurarRepositorio(ResultadoVacio());

        await CrearManejador()
            .Handle(new ConsultarUsuariosInternosConsulta(1, TamanioPaginaHu08, filtro, null),
                    CancellationToken.None);

        _repositorio.Verify(r => r.ConsultarUsuariosInternosAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            (RolUsuario?)null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
