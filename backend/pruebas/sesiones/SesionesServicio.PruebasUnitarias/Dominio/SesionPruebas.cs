using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// HU33 — Pruebas del agregado Sesion. Las validaciones de campos
// básicos (nombre, enums, fecha) ya no viven en la entidad: las cubre
// ValidadorCrearSesion en la capa de Aplicación. Aquí se prueban la
// construcción del agregado y las transiciones de estado del patrón
// State.
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

    [Fact]
    public void Crear_DebeAsignarEstadoProgramada_YDatosCompletos()
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
    }

    [Fact]
    public void Programada_Preparar_DebeIrA_EnPreparacion()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        sesion.Preparar();
        sesion.Estado.Should().Be(EstadoSesion.EnPreparacion);
    }

    [Fact]
    public void Programada_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Programada_Pausar_DebeFallar()
    {
        var sesion = SesionEnEstado(EstadoSesion.Programada);
        Action accion = () => sesion.Pausar();
        accion.Should().Throw<TransicionEstadoSesionInvalidaExcepcion>();
    }

    [Fact]
    public void EnPreparacion_Iniciar_DebeIrA_Activa()
    {
        var sesion = SesionEnEstado(EstadoSesion.EnPreparacion);
        sesion.Iniciar();
        sesion.Estado.Should().Be(EstadoSesion.Activa);
    }

    [Fact]
    public void EnPreparacion_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.EnPreparacion);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Activa_Pausar_DebeIrA_Pausada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Activa);
        sesion.Pausar();
        sesion.Estado.Should().Be(EstadoSesion.Pausada);
    }

    [Fact]
    public void Activa_Finalizar_DebeIrA_Finalizada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Activa);
        sesion.Finalizar();
        sesion.Estado.Should().Be(EstadoSesion.Finalizada);
    }

    [Fact]
    public void Activa_Cancelar_DebeIrA_Cancelada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Activa);
        sesion.Cancelar();
        sesion.Estado.Should().Be(EstadoSesion.Cancelada);
    }

    [Fact]
    public void Pausada_Reanudar_DebeIrA_Activa()
    {
        var sesion = SesionEnEstado(EstadoSesion.Pausada);
        sesion.Reanudar();
        sesion.Estado.Should().Be(EstadoSesion.Activa);
    }

    [Fact]
    public void Pausada_Finalizar_DebeIrA_Finalizada()
    {
        var sesion = SesionEnEstado(EstadoSesion.Pausada);
        sesion.Finalizar();
        sesion.Estado.Should().Be(EstadoSesion.Finalizada);
    }

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

    private static void InvocarTransicion(Sesion sesion, string accion)
    {
        var metodo = typeof(Sesion).GetMethod(accion)
            ?? throw new InvalidOperationException($"Método {accion} no existe en Sesion.");
        try
        {
            metodo.Invoke(sesion, null);
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
