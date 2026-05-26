using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Servicios.Usuarios;

public sealed class ResultadoCambiosUsuario
{
    public IReadOnlyCollection<string> CamposActualizados { get; }
    public DatosActualizacionUsuarioIdentidad DatosKeycloak { get; }
    public bool HuboCambiosDatosUsuario { get; }
    public bool CambiaContrasena { get; }
    public string? NuevaContrasena { get; }

    public ResultadoCambiosUsuario(
        IReadOnlyCollection<string> camposActualizados,
        DatosActualizacionUsuarioIdentidad datosKeycloak,
        bool huboCambiosDatosUsuario,
        bool cambiaContrasena,
        string? nuevaContrasena)
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
