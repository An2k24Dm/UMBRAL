using IdentidadServicio.Aplicacion.Comandos.CambiarContrasenaObligatoria;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

public class ValidadorCambiarContrasenaObligatoriaPruebas
{
    [Fact]
    public void Validar_InvocaReglasDeContrasenaYAceptaConfirmacionIgual()
    {
        var reglas = new Mock<IReglasValidacionUsuario>();
        var validador = new ValidadorCambiarContrasenaObligatoria(reglas.Object);
        var comando = Comando("Clave1!", "Clave1!");

        var resultado = validador.Validar(comando);

        resultado.EsValido.Should().BeTrue();
        reglas.Verify(r => r.ValidarContrasena("Clave1!", It.IsAny<ResultadoValidacion>()), Times.Once);
    }

    [Fact]
    public void Validar_ConfirmacionDistintaAgregaErrorEspecifico()
    {
        var reglas = new Mock<IReglasValidacionUsuario>();
        var validador = new ValidadorCambiarContrasenaObligatoria(reglas.Object);

        var resultado = validador.Validar(Comando("Clave1!", "Otra1!"));

        resultado.Errores.Should().ContainSingle(e =>
            e.Campo == MensajesValidacionUsuario.CampoConfirmacionContrasena &&
            e.Mensaje == MensajesValidacionUsuario.ContrasenasNoCoinciden);
    }

    [Fact]
    public void Validar_ConservaErroresDeReglasDeContrasena()
    {
        var reglas = new Mock<IReglasValidacionUsuario>();
        reglas.Setup(r => r.ValidarContrasena(It.IsAny<string?>(), It.IsAny<ResultadoValidacion>()))
            .Callback<string?, ResultadoValidacion>((_, resultado) =>
                resultado.Agregar(
                    MensajesValidacionUsuario.CampoNuevaContrasena,
                    MensajesValidacionUsuario.ContrasenaSinNumero));
        var validador = new ValidadorCambiarContrasenaObligatoria(reglas.Object);

        var resultado = validador.Validar(Comando("Clave!", "Clave!"));

        resultado.Errores.Should().ContainSingle(e =>
            e.Campo == MensajesValidacionUsuario.CampoNuevaContrasena &&
            e.Mensaje == MensajesValidacionUsuario.ContrasenaSinNumero);
    }

    private static CambiarContrasenaObligatoriaComando Comando(
        string nueva,
        string confirmacion) =>
        new("kc-1", new CambiarContrasenaObligatoriaDto
        {
            NuevaContrasena = nueva,
            ConfirmacionContrasena = confirmacion
        });
}
