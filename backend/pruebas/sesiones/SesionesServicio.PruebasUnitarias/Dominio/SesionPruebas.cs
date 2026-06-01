using System;
using System.Reflection;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Estados;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU33/HU34 — Pruebas del agregado Sesion como Context del patrón State.
//
// El test verifica:
//  * Construcción correcta del agregado (Sesion.Crear, Sesion.Rehidratar).
//  * Que la referencia interna _estadoActual quede inicializada con el
//    ConcreteState que corresponde a Estado.
//  * Que cada acción delegue en el estado actual y que las transiciones
//    inválidas lancen TransicionEstadoSesionInvalidaExcepcion.
public class SesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaProgramada = AhoraUtc.AddHours(1);
    private static readonly Guid CreadorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ContenidoJuegoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static Sesion SesionEnEstado(EstadoSesion estado)
        => Sesion.Rehidratar(
            Guid.NewGuid(),
            "Sesión piloto",
            TipoJuego.Trivia,
            ContenidoJuegoId,
            ModoSesion.Individual,
            estado,
            FechaProgramada,
            CreadorId,
            AhoraUtc);

    // Refleja el campo privado _estadoActual para verificar el tipo del
    // ConcreteState sin abrirlo en la API pública del agregado.
    private static IEstadoSesion EstadoActual(Sesion sesion)
    {
        var campo = typeof(Sesion).GetField("_estadoActual",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (IEstadoSesion)campo!.GetValue(sesion)!;
    }

    [Fact]
    public void Crear_DebeNacerProgramada_YConEstadoActualProgramada()
    {
        var sesion = Sesion.Crear(
            "Sesión piloto",
            TipoJuego.Trivia,
            ContenidoJuegoId,
            ModoSesion.Individual,
            FechaProgramada,
            CreadorId,
            AhoraUtc);

        sesion.Estado.Should().Be(EstadoSesion.Programada);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionProgramada");
        sesion.Nombre.Should().Be("Sesión piloto");
        sesion.TipoJuego.Should().Be(TipoJuego.Trivia);
        sesion.Modo.Should().Be(ModoSesion.Individual);
        sesion.ContenidoJuegoId.Should().Be(ContenidoJuegoId);
        sesion.FechaProgramada.Should().Be(FechaProgramada);
        sesion.FechaCreacion.Should().Be(AhoraUtc);
        sesion.CreadaPorUsuarioId.Should().Be(CreadorId);
        sesion.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Crear_DebeNormalizarNombreConRecorte()
    {
        var sesion = Sesion.Crear(
            "  Sesión piloto  ",
            TipoJuego.Trivia,
            ContenidoJuegoId,
            ModoSesion.Individual,
            FechaProgramada,
            CreadorId,
            AhoraUtc);

        sesion.Nombre.Should().Be("Sesión piloto");
    }

    [Fact]
    public void Entidad_NoDebeExponerCamposEliminados()
    {
        var tipo = typeof(Sesion);
        tipo.GetProperty("NombreContenido").Should().BeNull();
        tipo.GetProperty("CreadaPorNombreUsuario").Should().BeNull();
        tipo.GetProperty("ContenidoId").Should().BeNull();
        tipo.GetProperty("CreadaPorRol").Should().BeNull();
    }

    // -----------------------------------------------------------------
    // Programada → ...
    // -----------------------------------------------------------------

    [Fact]
    public void Programada_Preparar_DebeIrA_EnPreparacion()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        sesion.Preparar();
        sesion.Estado.Should().Be(EstadoSesion.EnPreparacion);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionEnPreparacion");
    }

    [Fact]
    public void Programada_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionCancelada");
    }

    [Fact]
    public void Programada_Iniciar_DebeLanzarTransicionInvalida()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        Action accion = () => sesion.Iniciar();
        accion.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    [Fact]
    public void Programada_Pausar_DebeLanzarTransicionInvalida()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        Action accion = () => sesion.Pausar();
        accion.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    // -----------------------------------------------------------------
    // EnPreparacion → ...
    // -----------------------------------------------------------------

    [Fact]
    public void EnPreparacion_Iniciar_DebeIrA_Activa()
    {
        var sesion = SesionEnEstado(EstadoSesion.EnPreparacion);
        sesion.Iniciar();
        sesion.Estado.Should().Be(EstadoSesion.Activa);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionActiva");
    }

    [Fact]
    public void EnPreparacion_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.EnPreparacion);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionCancelada");
    }

    // -----------------------------------------------------------------
    // Activa → ...
    // -----------------------------------------------------------------

    [Fact]
    public void Activa_Pausar_DebeIrA_Pausada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Activa);
        sesion.Pausar();
        sesion.Estado.Should().Be(EstadoSesion.Pausada);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionPausada");
    }

    [Fact]
    public void Activa_Finalizar_DebeIrA_Finalizada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Activa);
        sesion.Finalizar();
        sesion.Estado.Should().Be(EstadoSesion.Finalizada);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionFinalizada");
    }

    [Fact]
    public void Activa_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Activa);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    // -----------------------------------------------------------------
    // Pausada → ...
    // -----------------------------------------------------------------

    [Fact]
    public void Pausada_Reanudar_DebeIrA_Activa()
    {
        var sesion = SesionEnEstado(EstadoSesion.Pausada);
        sesion.Reanudar();
        sesion.Estado.Should().Be(EstadoSesion.Activa);
        EstadoActual(sesion).GetType().Name.Should().Be("EstadoSesionActiva");
    }

    [Fact]
    public void Pausada_Finalizar_DebeIrA_Finalizada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Pausada);
        sesion.Finalizar();
        sesion.Estado.Should().Be(EstadoSesion.Finalizada);
    }

    [Fact]
    public void Pausada_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Pausada);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    // -----------------------------------------------------------------
    // Estados terminales
    // -----------------------------------------------------------------

    [Theory]
    [InlineData(nameof(Sesion.Preparar))]
    [InlineData(nameof(Sesion.Iniciar))]
    [InlineData(nameof(Sesion.Pausar))]
    [InlineData(nameof(Sesion.Reanudar))]
    [InlineData(nameof(Sesion.Finalizar))]
    [InlineData(nameof(Sesion.Cancelar))]
    public void Finalizada_NoPermiteTransiciones(string accion)
    {
        var sesion = SesionEnEstado(EstadoSesion.Finalizada);
        Action ejecutar = () => InvocarTransicion(sesion, accion);
        ejecutar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    [Theory]
    [InlineData(nameof(Sesion.Preparar))]
    [InlineData(nameof(Sesion.Iniciar))]
    [InlineData(nameof(Sesion.Pausar))]
    [InlineData(nameof(Sesion.Reanudar))]
    [InlineData(nameof(Sesion.Finalizar))]
    [InlineData(nameof(Sesion.Cancelar))]
    public void Cancelada_NoPermiteTransiciones(string accion)
    {
        var sesion = SesionEnEstado(EstadoSesion.Cancelada);
        Action ejecutar = () => InvocarTransicion(sesion, accion);
        ejecutar.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    // -----------------------------------------------------------------
    // Rehidratación reconstruye el ConcreteState correcto
    // -----------------------------------------------------------------

    [Theory]
    [InlineData(EstadoSesion.Programada, "EstadoSesionProgramada")]
    [InlineData(EstadoSesion.EnPreparacion, "EstadoSesionEnPreparacion")]
    [InlineData(EstadoSesion.Activa, "EstadoSesionActiva")]
    [InlineData(EstadoSesion.Pausada, "EstadoSesionPausada")]
    [InlineData(EstadoSesion.Finalizada, "EstadoSesionFinalizada")]
    [InlineData(EstadoSesion.Cancelada, "EstadoSesionCancelada")]
    public void Rehidratar_DebeReconstruirElConcreteStateCorrespondiente(
        EstadoSesion estado, string nombreClaseEsperada)
    {
        // Los ConcreteState son `internal` al ensamblado de Dominio
        // (no deben exponerse a Aplicación ni a pruebas). Verificamos
        // el tipo concreto por nombre de clase.
        var sesion = SesionEnEstado(estado);
        sesion.Estado.Should().Be(estado);
        EstadoActual(sesion).GetType().Name.Should().Be(nombreClaseEsperada);
    }

    private static void InvocarTransicion(Sesion sesion, string accion)
    {
        var metodo = typeof(Sesion).GetMethod(accion)
            ?? throw new InvalidOperationException($"Método {accion} no existe en Sesion.");
        try
        {
            metodo.Invoke(sesion, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
