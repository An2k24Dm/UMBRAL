using IdentidadServicio.Aplicacion.Servicios.Usuarios;

namespace IdentidadServicio.PruebasUnitarias.Servicios.Usuarios;

public class MensajesContrasenaTemporalPruebas
{
    [Fact]
    public void CuerpoCreacion_IncluyeDatosDeAccesoRolYAdvertencia()
    {
        var cuerpo = MensajesContrasenaTemporal.CuerpoCreacion(
            "Ana Pérez",
            "ana.perez",
            "ana@umbral.local",
            "Temp1!",
            "Operador");

        cuerpo.Should().Contain("Hola Ana Pérez");
        cuerpo.Should().Contain("rol Operador");
        cuerpo.Should().Contain("Usuario: ana.perez");
        cuerpo.Should().Contain("Correo:  ana@umbral.local");
        cuerpo.Should().Contain("Contraseña temporal: Temp1!");
        cuerpo.Should().Contain("primera vez");
        cuerpo.Should().Contain("Equipo UMBRAL");
        MensajesContrasenaTemporal.AsuntoCreacion.Should().Be("Cuenta creada en UMBRAL");
    }

    [Fact]
    public void CuerpoReseteo_IncluyeDatosDeAccesoYContextoDeReseteo()
    {
        var cuerpo = MensajesContrasenaTemporal.CuerpoReseteo(
            "Luis Rojas",
            "luis.rojas",
            "luis@umbral.local",
            "Temp2!");

        cuerpo.Should().Contain("Hola Luis Rojas");
        cuerpo.Should().Contain("reseteo de tu contraseña");
        cuerpo.Should().Contain("Usuario: luis.rojas");
        cuerpo.Should().Contain("Correo:  luis@umbral.local");
        cuerpo.Should().Contain("Contraseña temporal: Temp2!");
        cuerpo.Should().Contain("Si no solicitaste este reseteo");
        MensajesContrasenaTemporal.AsuntoReseteo.Should().Be("Contraseña temporal de UMBRAL");
    }
}
