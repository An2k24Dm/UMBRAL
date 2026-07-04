namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRegistroLogsAplicacion
{
    void Informacion(
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades = null);

    void Advertencia(
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades = null);

    void Error(
        Exception excepcion,
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades = null);
}
