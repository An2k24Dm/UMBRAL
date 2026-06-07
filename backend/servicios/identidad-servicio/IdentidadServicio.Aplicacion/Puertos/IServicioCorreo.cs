namespace IdentidadServicio.Aplicacion.Puertos;

public interface IServicioCorreo
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoTextoPlano,
        CancellationToken cancelacion);
}
