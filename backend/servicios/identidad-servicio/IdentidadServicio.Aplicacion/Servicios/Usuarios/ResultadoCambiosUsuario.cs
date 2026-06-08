using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Servicios.Usuarios;

public sealed class ResultadoCambiosUsuario
{
    public IReadOnlyCollection<string> CamposActualizados { get; }
    public DatosActualizacionUsuarioIdentidad DatosKeycloak { get; }
    public bool HuboCambiosDatosUsuario { get; }
    // CambiaContrasena/NuevaContrasena solo se setean cuando el flujo es el
    // Participante editando su propio perfil. Para el flujo administrativo
    // de Operador/Administrador el AplicadorCambiosUsuario nunca los puebla.
    public bool CambiaContrasena { get; }
    public string? NuevaContrasena { get; }

    public ResultadoCambiosUsuario(
        IReadOnlyCollection<string> camposActualizados,
        DatosActualizacionUsuarioIdentidad datosKeycloak,
        bool huboCambiosDatosUsuario,
        bool cambiaContrasena = false,
        string? nuevaContrasena = null)
    {
        CamposActualizados = camposActualizados;
        DatosKeycloak = datosKeycloak;
        HuboCambiosDatosUsuario = huboCambiosDatosUsuario;
        CambiaContrasena = cambiaContrasena;
        NuevaContrasena = nuevaContrasena;
    }

    public bool RequiereGuardarBaseDatos => HuboCambiosDatosUsuario;

    public bool RequiereSincronizarKeycloak =>
        DatosKeycloak.TieneCambios || CambiaContrasena;
}
