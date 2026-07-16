using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Validaciones.OperacionSesion;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Validaciones;

public class ValidadorAccionJuegoSesionPruebas
{
    private static readonly DateTime Ahora = new(2026, 7, 16, 16, 0, 0, DateTimeKind.Utc);
    private readonly ValidadorAccionJuegoSesion _validador = new();

    [Fact]
    public void Validar_SesionActiva_NoLanza()
    {
        var sesion = Sesion(EstadoSesion.Activa);

        Action accion = () => _validador.Validar(sesion);

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(EstadoSesion.Pausada, "pausada")]
    [InlineData(EstadoSesion.Cancelada, "cancelada")]
    [InlineData(EstadoSesion.Finalizada, "finalizó")]
    [InlineData(EstadoSesion.Programada, "aún no está activa")]
    [InlineData(EstadoSesion.EnPreparacion, "aún no está activa")]
    public void Validar_SesionNoActiva_LanzaMensajeEspecifico(
        EstadoSesion estado,
        string fragmentoMensaje)
    {
        var sesion = Sesion(estado);

        Action accion = () => _validador.Validar(sesion);

        accion.Should().Throw<OperacionSesionInvalidaExcepcion>()
            .WithMessage("*" + fragmentoMensaje + "*");
    }

    private static SesionIndividual Sesion(EstadoSesion estado) =>
        SesionIndividual.Rehidratar(
            Guid.NewGuid(),
            "Sesion",
            "Demo",
            estado,
            Ahora.AddHours(1),
            "COD",
            Guid.NewGuid(),
            Ahora,
            estado is EstadoSesion.Activa or EstadoSesion.Pausada or EstadoSesion.Finalizada
                ? Ahora
                : null,
            estado == EstadoSesion.Finalizada ? Ahora.AddHours(2) : null,
            5);
}
